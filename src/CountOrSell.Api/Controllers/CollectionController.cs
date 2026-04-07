using System.Security.Claims;
using CountOrSell.Api.Filters;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain;
using CountOrSell.Domain.Dtos.Requests;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/collection")]
[Authorize]
public class CollectionController : ControllerBase
{
    private readonly ICollectionRepository _collection;
    private readonly ICardRepository _cards;
    private readonly IMetricsService _metrics;
    private readonly IUserRepository _users;
    private readonly ITcgPlayerService _tcgPlayer;

    public CollectionController(
        ICollectionRepository collection,
        ICardRepository cards,
        IMetricsService metrics,
        IUserRepository users,
        ITcgPlayerService tcgPlayer)
    {
        _collection = collection;
        _cards = cards;
        _metrics = metrics;
        _users = users;
        _tcgPlayer = tcgPlayer;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin =>
        User.IsInRole("Admin");

    private Guid ResolveUserId(Guid? requestedUserId)
    {
        if (requestedUserId.HasValue && IsAdmin)
            return requestedUserId.Value;
        return CurrentUserId;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? userId, [FromQuery] CollectionFilter? filter, CancellationToken ct)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        var targetUserId = ResolveUserId(userId);
        var effectiveFilter = filter ?? new CollectionFilter();

        List<CollectionEntry> entries;
        if (HasFilters(effectiveFilter))
            entries = await _collection.GetByUserFilteredAsync(targetUserId, effectiveFilter, ct);
        else
            entries = await _collection.GetByUserAsync(targetUserId, ct);

        var identifiers = entries.Select(e => e.CardIdentifier).Distinct().ToList();
        var summaries = await _cards.GetSummaryByIdentifiersAsync(identifiers, ct);
        var oracleUrls = await _cards.GetOracleRulingUrlsByIdentifiersAsync(identifiers, ct);
        return Ok(entries.Select(e =>
        {
            summaries.TryGetValue(e.CardIdentifier, out var summary);
            return MapEntry(e, summary.Name, summary.MarketValue, summary.SetCode, oracleUrls.GetValueOrDefault(e.CardIdentifier));
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CollectionEntryRequest request, CancellationToken ct)
    {
        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        var cardId = request.CardIdentifier.ToLowerInvariant();
        if (!CardIdentifierValidator.IsValid(cardId))
            return BadRequest(new { error = $"Invalid card identifier: {request.CardIdentifier.ToUpperInvariant()}. Expected format: set code (3-4 alphanumeric) followed by card number (3 digits, or 4 digits >= 1000)." });
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            CardIdentifier = cardId,
            TreatmentKey = request.Treatment,
            Quantity = request.Quantity,
            Condition = condition,
            Autographed = request.Autographed,
            AcquisitionDate = request.AcquisitionDate,
            AcquisitionPrice = request.AcquisitionPrice,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _collection.CreateAsync(entry, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapEntry(created));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _collection.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId && !IsAdmin) return Forbid();
        return Ok(MapEntry(entry));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CollectionEntryRequest request, CancellationToken ct)
    {
        var entry = await _collection.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        entry.TreatmentKey = request.Treatment;
        entry.Quantity = request.Quantity;
        entry.Condition = condition;
        entry.Autographed = request.Autographed;
        entry.AcquisitionDate = request.AcquisitionDate;
        entry.AcquisitionPrice = request.AcquisitionPrice;
        entry.Notes = request.Notes;
        entry.UpdatedAt = DateTime.UtcNow;

        var updated = await _collection.UpdateAsync(entry, ct);
        return Ok(MapEntry(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entry = await _collection.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        await _collection.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/quantity")]
    public async Task<IActionResult> AdjustQuantity(Guid id, [FromBody] int delta, CancellationToken ct)
    {
        var entry = await _collection.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        var newQuantity = entry.Quantity + delta;
        if (newQuantity < 1)
            return BadRequest(new { error = "Quantity cannot be less than 1." });

        entry.Quantity = newQuantity;
        entry.UpdatedAt = DateTime.UtcNow;
        var updated = await _collection.UpdateAsync(entry, ct);
        return Ok(MapEntry(updated));
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics([FromQuery] Guid? userId, [FromQuery] CollectionFilter? filter, CancellationToken ct)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        CollectionFilter effectiveFilter = filter ?? new CollectionFilter();

        if (IsAdmin && !userId.HasValue)
        {
            var aggregate = await _metrics.GetAggregateMetricsAsync(effectiveFilter, ct);
            return Ok(aggregate);
        }

        var targetUserId = ResolveUserId(userId);
        var result = await _metrics.GetMetricsAsync(targetUserId, effectiveFilter, ct);
        return Ok(result);
    }

    [HttpGet("completion")]
    public async Task<IActionResult> GetAllSetCompletion([FromQuery] Guid? userId, [FromQuery] bool regularOnly = false, CancellationToken ct = default)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        var targetUserId = ResolveUserId(userId);
        var results = await _metrics.GetAllSetCompletionAsync(targetUserId, regularOnly, ct);
        return Ok(results);
    }

    [HttpGet("completion/{setCode}")]
    public async Task<IActionResult> GetSetCompletion(string setCode, [FromQuery] Guid? userId, [FromQuery] bool regularOnly = false, CancellationToken ct = default)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        var targetUserId = ResolveUserId(userId);
        var result = await _metrics.GetSetCompletionAsync(targetUserId, setCode, regularOnly, ct);
        return Ok(result);
    }

    // GET /api/collection/reserved
    // Returns all collection entries for cards on the Reserved List, enriched with card data.
    [HttpGet("reserved")]
    public async Task<IActionResult> GetReserved([FromQuery] Guid? userId, CancellationToken ct)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        var targetUserId = ResolveUserId(userId);
        var entries = await _collection.GetReservedEntriesForUserAsync(targetUserId, ct);
        return Ok(entries);
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkIdsRequest request, CancellationToken ct)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return BadRequest(new { error = "At least one id is required." });
        var deleted = await _collection.BulkDeleteAsync(request.Ids, CurrentUserId, ct);
        return Ok(new { deleted });
    }

    [HttpPost("bulk-set-treatment")]
    public async Task<IActionResult> BulkSetTreatment([FromBody] BulkSetTreatmentRequest request, CancellationToken ct)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return BadRequest(new { error = "At least one id is required." });
        if (string.IsNullOrEmpty(request.Treatment))
            return BadRequest(new { error = "Treatment is required." });
        var updated = await _collection.BulkSetTreatmentAsync(request.Ids, CurrentUserId, request.Treatment, ct);
        return Ok(new { updated });
    }

    [HttpPost("bulk-set-acquisition-date")]
    public async Task<IActionResult> BulkSetAcquisitionDate([FromBody] BulkSetAcquisitionDateRequest request, CancellationToken ct)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return BadRequest(new { error = "At least one id is required." });
        var updated = await _collection.BulkSetAcquisitionDateAsync(request.Ids, CurrentUserId, request.AcquisitionDate, ct);
        return Ok(new { updated });
    }

    [HttpPost("bulk-add-set")]
    public async Task<IActionResult> BulkAddSet([FromBody] BulkAddSetRequest request, CancellationToken ct)
    {
        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        var setCode = request.SetCode.ToLowerInvariant();
        var cards = await _cards.GetBySetCodeAsync(setCode, ct);
        if (cards.Count == 0)
            return BadRequest(new { error = $"No cards found for set {request.SetCode.ToUpperInvariant()}." });

        var ownedIdentifiers = await _collection.GetOwnedIdentifiersBySetAsync(CurrentUserId, setCode, ct);

        var now = DateTime.UtcNow;
        var toAdd = cards
            .Where(c => !ownedIdentifiers.Contains(c.Identifier))
            .Select(c => new CollectionEntry
            {
                Id = Guid.NewGuid(),
                UserId = CurrentUserId,
                CardIdentifier = c.Identifier,
                TreatmentKey = request.Treatment,
                Quantity = 1,
                Condition = condition,
                Autographed = false,
                AcquisitionDate = request.AcquisitionDate,
                AcquisitionPrice = request.AcquisitionPrice ?? (c.CurrentMarketValue ?? 0),
                Notes = null,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        if (toAdd.Count > 0)
            await _collection.BulkCreateAsync(toAdd, ct);

        return Ok(new { added = toAdd.Count, skipped = ownedIdentifiers.Count });
    }

    [HttpPost("refresh-price/{cardIdentifier}")]
    [DemoLocked]
    public async Task<IActionResult> RefreshPrice(string cardIdentifier, CancellationToken ct)
    {
        if (!_tcgPlayer.IsConfigured)
            return BadRequest(new { error = "TCGPlayer API key is not configured." });

        var price = await _tcgPlayer.GetMarketValueAsync(cardIdentifier.ToLowerInvariant(), ct);
        if (price == null)
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "TCGPlayer query returned no price." });

        return Ok(new { cardIdentifier = cardIdentifier.ToUpperInvariant(), marketValue = price });
    }

    private static bool HasFilters(CollectionFilter filter) =>
        filter.SetCode != null || filter.Color != null || filter.Condition != null ||
        filter.CardType != null || filter.Treatment != null || filter.Autographed.HasValue;

    private static bool TryParseCondition(string value, out CardCondition result) =>
        Enum.TryParse(value, true, out result);

    private static object MapEntry(
        CollectionEntry e,
        string? cardName = null,
        decimal? marketValue = null,
        string? setCode = null,
        string? oracleRulingUrl = null) => new
    {
        e.Id,
        e.UserId,
        CardIdentifier = e.CardIdentifier.ToUpperInvariant(),
        CardName = cardName,
        SetCode = setCode?.ToUpperInvariant(),
        MarketValue = marketValue,
        e.TreatmentKey,
        e.Quantity,
        Condition = e.Condition.ToString(),
        e.Autographed,
        e.AcquisitionDate,
        e.AcquisitionPrice,
        e.Notes,
        e.CreatedAt,
        e.UpdatedAt,
        OracleRulingUrl = oracleRulingUrl
    };
}
