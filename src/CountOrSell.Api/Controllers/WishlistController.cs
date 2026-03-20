using System.Security.Claims;
using CountOrSell.Api.Filters;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain;
using CountOrSell.Domain.Dtos.Requests;
using CountOrSell.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistRepository _wishlist;

    public WishlistController(IWishlistRepository wishlist)
    {
        _wishlist = wishlist;
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
            MarketValue = r.Card?.CurrentMarketValue ?? 0,
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

        var entry = new WishlistEntry
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            CardIdentifier = cardId,
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
