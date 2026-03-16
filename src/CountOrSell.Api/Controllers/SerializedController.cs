using System.Security.Claims;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos.Requests;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/serialized")]
[Authorize]
public class SerializedController : ControllerBase
{
    private readonly ISerializedRepository _serialized;

    public SerializedController(ISerializedRepository serialized)
    {
        _serialized = serialized;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin =>
        User.IsInRole("Admin");

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? userId, CancellationToken ct)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        var targetUserId = userId.HasValue ? userId.Value : CurrentUserId;
        var entries = await _serialized.GetByUserAsync(targetUserId, ct);
        return Ok(entries.Select(MapEntry));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SerializedEntryRequest request, CancellationToken ct)
    {
        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        var entry = new SerializedEntry
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            CardIdentifier = request.CardIdentifier.ToLowerInvariant(),
            TreatmentKey = request.Treatment,
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

        var created = await _serialized.CreateAsync(entry, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapEntry(created));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _serialized.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId && !IsAdmin) return Forbid();
        return Ok(MapEntry(entry));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SerializedEntryRequest request, CancellationToken ct)
    {
        var entry = await _serialized.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        entry.TreatmentKey = request.Treatment;
        entry.SerialNumber = request.SerialNumber;
        entry.PrintRunTotal = request.PrintRunTotal;
        entry.Condition = condition;
        entry.Autographed = request.Autographed;
        entry.AcquisitionDate = request.AcquisitionDate;
        entry.AcquisitionPrice = request.AcquisitionPrice;
        entry.Notes = request.Notes;
        entry.UpdatedAt = DateTime.UtcNow;

        var updated = await _serialized.UpdateAsync(entry, ct);
        return Ok(MapEntry(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entry = await _serialized.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        await _serialized.DeleteAsync(id, ct);
        return NoContent();
    }

    private static bool TryParseCondition(string value, out CardCondition result) =>
        Enum.TryParse(value, true, out result);

    private static object MapEntry(SerializedEntry e) => new
    {
        e.Id,
        e.UserId,
        CardIdentifier = e.CardIdentifier.ToUpperInvariant(),
        e.TreatmentKey,
        e.SerialNumber,
        e.PrintRunTotal,
        Condition = e.Condition.ToString(),
        e.Autographed,
        e.AcquisitionDate,
        e.AcquisitionPrice,
        e.Notes,
        e.CreatedAt,
        e.UpdatedAt
    };
}
