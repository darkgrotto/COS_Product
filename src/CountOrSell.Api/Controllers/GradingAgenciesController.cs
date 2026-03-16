using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos.Requests;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/grading-agencies")]
[Authorize]
public class GradingAgenciesController : ControllerBase
{
    private readonly IGradingAgencyRepository _agencies;
    private readonly ISlabRepository _slabs;

    public GradingAgenciesController(IGradingAgencyRepository agencies, ISlabRepository slabs)
    {
        _agencies = agencies;
        _slabs = slabs;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var agencies = await _agencies.GetAllAsync(ct);
        return Ok(agencies.Select(MapAgency));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] GradingAgencyRequest request, CancellationToken ct)
    {
        var code = request.Code.ToLowerInvariant();
        var existing = await _agencies.GetByCodeAsync(code, ct);
        if (existing != null)
            return Conflict(new { error = $"Agency with code '{code.ToUpperInvariant()}' already exists." });

        var agency = new GradingAgency
        {
            Code = code,
            FullName = request.FullName,
            ValidationUrlTemplate = request.ValidationUrlTemplate,
            SupportsDirectLookup = request.SupportsDirectLookup,
            Source = AgencySource.Local,
            Active = true
        };

        var created = await _agencies.CreateAsync(agency, ct);
        return Created($"/api/grading-agencies/{created.Code}", MapAgency(created));
    }

    [HttpPatch("{code}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Patch(string code, [FromBody] GradingAgencyPatchRequest request, CancellationToken ct)
    {
        var codeLower = code.ToLowerInvariant();
        var agency = await _agencies.GetByCodeAsync(codeLower, ct);
        if (agency == null) return NotFound();

        if (agency.Source == AgencySource.Canonical)
            return Forbid();

        if (request.FullName != null) agency.FullName = request.FullName;
        if (request.ValidationUrlTemplate != null) agency.ValidationUrlTemplate = request.ValidationUrlTemplate;
        if (request.SupportsDirectLookup.HasValue) agency.SupportsDirectLookup = request.SupportsDirectLookup.Value;

        var updated = await _agencies.UpdateAsync(agency, ct);
        return Ok(MapAgency(updated));
    }

    [HttpDelete("{code}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string code, [FromBody] GradingAgencyDeleteRequest? deleteRequest, CancellationToken ct)
    {
        var codeLower = code.ToLowerInvariant();
        var agency = await _agencies.GetByCodeAsync(codeLower, ct);
        if (agency == null) return NotFound();

        if (agency.Source == AgencySource.Canonical)
            return Forbid();

        var recordCount = await _slabs.CountByAgencyCodeAsync(codeLower, ct);
        if (recordCount > 0)
        {
            if (string.IsNullOrWhiteSpace(deleteRequest?.ReplacementCode))
                return Conflict(new { requiresReplacement = true, recordCount });

            var replacementCode = deleteRequest.ReplacementCode.ToLowerInvariant();
            var replacement = await _agencies.GetByCodeAsync(replacementCode, ct);
            if (replacement == null)
                return BadRequest(new { error = $"Replacement agency '{replacementCode.ToUpperInvariant()}' not found." });

            await _slabs.RemapAgencyCodeAsync(codeLower, replacementCode, ct);
        }

        await _agencies.DeleteAsync(codeLower, ct);
        return NoContent();
    }

    private static object MapAgency(GradingAgency a) => new
    {
        Code = a.Code.ToUpperInvariant(),
        a.FullName,
        a.ValidationUrlTemplate,
        a.SupportsDirectLookup,
        Source = a.Source.ToString(),
        a.Active
    };
}
