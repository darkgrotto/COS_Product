using System.Security.Claims;
using CountOrSell.Api.Filters;
using CountOrSell.Api.Services;
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
    private readonly ICollectionImportExportService _importExport;
    private readonly ITreatmentValidator _treatments;

    public CollectionController(
        ICollectionRepository collection,
        ICardRepository cards,
        IMetricsService metrics,
        IUserRepository users,
        ITcgPlayerService tcgPlayer,
        ICollectionImportExportService importExport,
        ITreatmentValidator treatments)
    {
        _collection = collection;
        _cards = cards;
        _metrics = metrics;
        _users = users;
        _tcgPlayer = tcgPlayer;
        _importExport = importExport;
        _treatments = treatments;
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
        var treatmentPrices = await _cards.GetPricesByIdentifiersAsync(identifiers, ct);
        return Ok(entries.Select(e =>
        {
            summaries.TryGetValue(e.CardIdentifier, out var summary);
            decimal? mv = treatmentPrices.TryGetValue(e.CardIdentifier, out var tPrices) &&
                          tPrices.TryGetValue(e.TreatmentKey, out var tp)
                ? tp
                : summary.MarketValue;
            return MapEntry(e, summary.Name, mv, summary.SetCode, oracleUrls.GetValueOrDefault(e.CardIdentifier));
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
        if (!await _treatments.IsValidAsync(request.Treatment, ct))
            return BadRequest(new { error = $"Unknown treatment: {request.Treatment}" });
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

        if (!await _treatments.IsValidAsync(request.Treatment, ct))
            return BadRequest(new { error = $"Unknown treatment: {request.Treatment}" });

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
    public async Task<IActionResult> GetAllSetCompletion(
        [FromQuery] Guid? userId,
        [FromQuery] bool regularOnly = false,
        [FromQuery] CollectionFilter? filter = null,
        CancellationToken ct = default)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        var targetUserId = ResolveUserId(userId);
        var results = await _metrics.GetAllSetCompletionAsync(targetUserId, regularOnly, filter, ct);
        return Ok(results);
    }

    [HttpGet("top-cards")]
    public async Task<IActionResult> GetTopCards(
        [FromQuery] Guid? userId,
        [FromQuery] string metric = "value",
        [FromQuery] int limit = 25,
        [FromQuery] int offset = 0,
        [FromQuery] CollectionFilter? filter = null,
        CancellationToken ct = default)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        if (limit < 1 || limit > 100) limit = 25;
        if (offset < 0) offset = 0;
        if (metric != "value" && metric != "frequency") metric = "value";

        var targetUserId = ResolveUserId(userId);
        var effectiveFilter = filter ?? new CollectionFilter();
        var (results, totalCount) = await _metrics.GetTopCardsAsync(targetUserId, metric, limit, offset, effectiveFilter, ct);
        return Ok(new { results, totalCount, limit, offset });
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
        if (!await _treatments.IsValidAsync(request.Treatment, ct))
            return BadRequest(new { error = $"Unknown treatment: {request.Treatment}" });
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

        if (!await _treatments.IsValidAsync(request.Treatment, ct))
            return BadRequest(new { error = $"Unknown treatment: {request.Treatment}" });

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

    [HttpPost("bulk-add-sets")]
    public async Task<IActionResult> BulkAddSets([FromBody] BulkAddSetsRequest request, CancellationToken ct)
    {
        if (request.SetCodes == null || request.SetCodes.Count == 0)
            return BadRequest(new { error = "At least one set code is required." });

        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        if (!await _treatments.IsValidAsync(request.Treatment, ct))
            return BadRequest(new { error = $"Unknown treatment: {request.Treatment}" });

        var now = DateTime.UtcNow;
        var bySet = new List<object>();
        var totalAdded = 0;
        var totalSkipped = 0;

        foreach (var rawCode in request.SetCodes.Distinct())
        {
            var setCode = rawCode.ToLowerInvariant();
            var cards = await _cards.GetBySetCodeAsync(setCode, ct);
            if (cards.Count == 0)
            {
                bySet.Add(new { setCode = rawCode.ToUpperInvariant(), added = 0, skipped = 0, notFound = true });
                continue;
            }

            var ownedIdentifiers = await _collection.GetOwnedIdentifiersBySetAsync(CurrentUserId, setCode, ct);
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

            bySet.Add(new { setCode = rawCode.ToUpperInvariant(), added = toAdd.Count, skipped = ownedIdentifiers.Count, notFound = false });
            totalAdded += toAdd.Count;
            totalSkipped += ownedIdentifiers.Count;
        }

        return Ok(new { totalAdded, totalSkipped, bySet });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string format = "cos",
        [FromQuery] CollectionFilter? filter = null,
        CancellationToken ct = default)
    {
        var fmt = ParseFormat(format);
        if (filter != null && HasFilters(filter))
        {
            var (fData, fFileName) = await _importExport.ExportFilteredAsync(CurrentUserId, fmt, filter, ct);
            return File(fData, "text/csv; charset=utf-8", fFileName);
        }
        var (data, fileName) = await _importExport.ExportAsync(CurrentUserId, fmt, ct);
        return File(data, "text/csv; charset=utf-8", fileName);
    }

    [HttpPost("import")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<IActionResult> Import(
        IFormFile file,
        [FromQuery] string format = "cos",
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var fmt = ParseFormat(format);
        using var stream = file.OpenReadStream();
        var result = await _importExport.ImportAsync(CurrentUserId, fmt, stream, ct);

        return Ok(new
        {
            result.Added,
            result.Skipped,
            result.Failed,
            result.Failures,
        });
    }

    private static CollectionExportFormat ParseFormat(string format) =>
        format.ToLowerInvariant() switch
        {
            "moxfield"    => CollectionExportFormat.Moxfield,
            "deckbox"     => CollectionExportFormat.Deckbox,
            "tcgplayer"   => CollectionExportFormat.TcgPlayer,
            "dragonshield" => CollectionExportFormat.DragonShield,
            "manabox"     => CollectionExportFormat.Manabox,
            _             => CollectionExportFormat.Cos,
        };

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
        filter.CardType != null || filter.Treatment != null || filter.Autographed.HasValue ||
        filter.IsReserved.HasValue || filter.HasPhyrexianMana.HasValue || filter.HasHybridMana.HasValue;

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
