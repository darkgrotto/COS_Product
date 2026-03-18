using System.Security.Claims;
using CountOrSell.Data.Repositories;
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
