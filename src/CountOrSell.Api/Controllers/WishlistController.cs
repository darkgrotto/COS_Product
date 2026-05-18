using System.Security.Claims;
using CountOrSell.Api.Filters;
using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain;
using CountOrSell.Domain.Dtos.Requests;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistRepository _wishlist;
    private readonly ITreatmentValidator _treatments;
    private readonly IWishlistImportExportService _importExport;

    public WishlistController(
        IWishlistRepository wishlist,
        ITreatmentValidator treatments,
        IWishlistImportExportService importExport)
    {
        _wishlist = wishlist;
        _treatments = treatments;
        _importExport = importExport;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CollectionFilter filter, CancellationToken ct)
    {
        var rows = await _wishlist.GetByUserWithCardsAsync(CurrentUserId, filter, ct);

        decimal totalValue = rows.Sum(r => r.Card?.CurrentMarketValue ?? 0);
        var entriesWithValue = rows.Select(r => new
        {
            r.Entry.Id,
            CardIdentifier = r.Entry.CardIdentifier.ToUpperInvariant(),
            CardName = r.Card?.Name,
            SetCode = r.Card?.SetCode?.ToUpperInvariant(),
            Color = r.Card?.Color,
            CardType = r.Card?.CardType,
            MarketValue = r.Card?.CurrentMarketValue ?? 0,
            TreatmentKey = r.Entry.TreatmentKey,
            r.Entry.CreatedAt
        }).ToList();

        return Ok(new { totalValue, entries = entriesWithValue });
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] WishlistRequest request, CancellationToken ct)
    {
        var cardId = request.CardIdentifier.ToLowerInvariant();
        if (!CardIdentifierValidator.IsValid(cardId))
            return BadRequest(new { error = $"Invalid card identifier: {request.CardIdentifier.ToUpperInvariant()}. Expected format: set code (3-4 alphanumeric) followed by card number (3 digits, or 4 digits >= 1000)." });

        var treatmentKey = string.IsNullOrWhiteSpace(request.TreatmentKey) ? "regular" : request.TreatmentKey.Trim().ToLowerInvariant();
        if (!await _treatments.IsValidAsync(treatmentKey, ct))
            return BadRequest(new { error = $"Unknown treatment: {treatmentKey}" });

        var entry = new WishlistEntry
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            CardIdentifier = cardId,
            TreatmentKey = treatmentKey,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _wishlist.CreateAsync(entry, ct);
        return Created($"/api/wishlist/{created.Id}", new
        {
            created.Id,
            CardIdentifier = created.CardIdentifier.ToUpperInvariant(),
            created.CreatedAt
        });
    }

    [HttpGet("export/tcgplayer")]
    [DemoLocked]
    public async Task<IActionResult> ExportTcgPlayer(CancellationToken ct)
    {
        var rows = await _wishlist.GetByUserWithCardsAsync(CurrentUserId, new CollectionFilter(), ct);
        var lines = rows
            .Where(r => r.Card != null)
            .Select(r => $"1 {r.Card!.Name} [{r.Card.SetCode.ToUpperInvariant()}]");
        return Content(string.Join("\n", lines), "text/plain");
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkIdsRequest request, CancellationToken ct)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return BadRequest(new { error = "At least one id is required." });
        var deleted = await _wishlist.BulkDeleteAsync(request.Ids, CurrentUserId, ct);
        return Ok(new { deleted });
    }

    [HttpGet("import-template")]
    public IActionResult ImportTemplate()
    {
        var (data, fileName) = _importExport.GenerateTemplate();
        return File(data, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var (data, fileName) = await _importExport.ExportAsync(CurrentUserId, ct);
        return File(data, "text/csv; charset=utf-8", fileName);
    }

    [HttpPost("import")]
    [DemoLocked]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        using var stream = file.OpenReadStream();
        var result = await _importExport.ImportAsync(CurrentUserId, stream, ct);
        return Ok(new
        {
            result.Added,
            result.Skipped,
            result.Failed,
            result.Failures,
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var entry = await _wishlist.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        await _wishlist.DeleteAsync(id, ct);
        return NoContent();
    }
}
