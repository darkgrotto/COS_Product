using System.Security.Claims;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain;
using CountOrSell.Domain.Dtos.Requests;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/slabs")]
[Authorize]
public class SlabsController : ControllerBase
{
    private readonly ISlabRepository _slabs;
    private readonly ICardRepository _cards;

    public SlabsController(ISlabRepository slabs, ICardRepository cards)
    {
        _slabs = slabs;
        _cards = cards;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin =>
        User.IsInRole("Admin");

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? userId, [FromQuery] CollectionFilter filter, CancellationToken ct)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        var targetUserId = userId.HasValue ? userId.Value : CurrentUserId;

        List<SlabEntry> entries;
        if (HasFilters(filter))
            entries = await _slabs.GetByUserFilteredAsync(targetUserId, filter, ct);
        else
            entries = await _slabs.GetByUserAsync(targetUserId, ct);

        var identifiers = entries.Select(e => e.CardIdentifier).Distinct().ToList();
        var summaries = await _cards.GetSummaryByIdentifiersAsync(identifiers, ct);
        return Ok(entries.Select(e =>
        {
            summaries.TryGetValue(e.CardIdentifier, out var s);
            return MapEntry(e, s.Name, s.MarketValue, s.SetCode);
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SlabEntryRequest request, CancellationToken ct)
    {
        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        if (request.SerialNumber.HasValue && !request.PrintRunTotal.HasValue)
            return BadRequest(new { error = "PrintRunTotal is required when SerialNumber is provided." });

        var cardId = request.CardIdentifier.ToLowerInvariant();
        if (!CardIdentifierValidator.IsValid(cardId))
            return BadRequest(new { error = $"Invalid card identifier: {request.CardIdentifier.ToUpperInvariant()}. Expected format: set code (3-4 alphanumeric) followed by card number (3 digits, or 4 digits >= 1000)." });

        var entry = new SlabEntry
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            CardIdentifier = cardId,
            TreatmentKey = request.Treatment,
            GradingAgencyCode = request.GradingAgency.ToLowerInvariant(),
            Grade = request.Grade,
            CertificateNumber = request.CertificateNumber,
            SerialNumber = request.SerialNumber,
            PrintRunTotal = request.PrintRunTotal,
            Condition = condition,
            Autographed = request.Autographed,
            AcquisitionDate = request.AcquisitionDate,
            AcquisitionPrice = request.AcquisitionPrice,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _slabs.CreateAsync(entry, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapEntry(created));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _slabs.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId && !IsAdmin) return Forbid();
        return Ok(MapEntry(entry));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SlabEntryRequest request, CancellationToken ct)
    {
        var entry = await _slabs.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        if (request.SerialNumber.HasValue && !request.PrintRunTotal.HasValue)
            return BadRequest(new { error = "PrintRunTotal is required when SerialNumber is provided." });

        entry.TreatmentKey = request.Treatment;
        entry.GradingAgencyCode = request.GradingAgency.ToLowerInvariant();
        entry.Grade = request.Grade;
        entry.CertificateNumber = request.CertificateNumber;
        entry.SerialNumber = request.SerialNumber;
        entry.PrintRunTotal = request.PrintRunTotal;
        entry.Condition = condition;
        entry.Autographed = request.Autographed;
        entry.AcquisitionDate = request.AcquisitionDate;
        entry.AcquisitionPrice = request.AcquisitionPrice;
        entry.Notes = request.Notes;
        entry.UpdatedAt = DateTime.UtcNow;

        var updated = await _slabs.UpdateAsync(entry, ct);
        return Ok(MapEntry(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entry = await _slabs.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        await _slabs.DeleteAsync(id, ct);
        return NoContent();
    }

    private static bool HasFilters(CollectionFilter filter) =>
        filter.SetCode != null || filter.Treatment != null || filter.Condition != null ||
        filter.Autographed.HasValue || filter.GradingAgency != null;

    private static bool TryParseCondition(string value, out CardCondition result) =>
        Enum.TryParse(value, true, out result);

    private static object MapEntry(
        SlabEntry e,
        string? cardName = null,
        decimal? marketValue = null,
        string? setCode = null) => new
    {
        e.Id,
        e.UserId,
        CardIdentifier = e.CardIdentifier.ToUpperInvariant(),
        CardName = cardName,
        SetCode = setCode?.ToUpperInvariant(),
        MarketValue = marketValue,
        e.TreatmentKey,
        GradingAgencyCode = e.GradingAgencyCode.ToUpperInvariant(),
        e.Grade,
        e.CertificateNumber,
        e.SerialNumber,
        e.PrintRunTotal,
        Condition = e.Condition.ToString(),
        e.Autographed,
        e.AcquisitionDate,
        e.AcquisitionPrice,
        e.Notes,
        e.CreatedAt,
        e.UpdatedAt,
    };
}
