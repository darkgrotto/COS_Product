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
[Route("api/serialized")]
[Authorize]
public class SerializedController : ControllerBase
{
    private readonly ISerializedRepository _serialized;
    private readonly ICardRepository _cards;

    public SerializedController(ISerializedRepository serialized, ICardRepository cards)
    {
        _serialized = serialized;
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

        List<SerializedEntry> entries;
        if (HasFilters(filter))
            entries = await _serialized.GetByUserFilteredAsync(targetUserId, filter, ct);
        else
            entries = await _serialized.GetByUserAsync(targetUserId, ct);

        var identifiers = entries.Select(e => e.CardIdentifier).Distinct().ToList();
        var summaries = await _cards.GetSummaryByIdentifiersAsync(identifiers, ct);
        return Ok(entries.Select(e =>
        {
            summaries.TryGetValue(e.CardIdentifier, out var s);
            return MapEntry(e, s.Name, s.MarketValue, s.SetCode);
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SerializedEntryRequest request, CancellationToken ct)
    {
        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        var cardId = request.CardIdentifier.ToLowerInvariant();
        if (!CardIdentifierValidator.IsValid(cardId))
            return BadRequest(new { error = $"Invalid card identifier: {request.CardIdentifier.ToUpperInvariant()}. Expected format: set code (3-4 alphanumeric) followed by card number (3 digits, or 4 digits >= 1000)." });

        var entry = new SerializedEntry
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            CardIdentifier = cardId,
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

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkIdsRequest request, CancellationToken ct)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return BadRequest(new { error = "At least one id is required." });
        var deleted = await _serialized.BulkDeleteAsync(request.Ids, CurrentUserId, ct);
        return Ok(new { deleted });
    }

    private static bool HasFilters(CollectionFilter filter) =>
        filter.SetCode != null || filter.Treatment != null || filter.Condition != null ||
        filter.Autographed.HasValue;

    private static bool TryParseCondition(string value, out CardCondition result) =>
        Enum.TryParse(value, true, out result);

    private static object MapEntry(
        SerializedEntry e,
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
