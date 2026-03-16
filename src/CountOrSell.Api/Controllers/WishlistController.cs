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
    private readonly ICardRepository _cards;

    public WishlistController(IWishlistRepository wishlist, ICardRepository cards)
    {
        _wishlist = wishlist;
        _cards = cards;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var entries = await _wishlist.GetByUserAsync(CurrentUserId, ct);

        var entriesWithValue = new List<object>(entries.Count);
        decimal totalValue = 0;

        foreach (var entry in entries)
        {
            var card = await _cards.GetByIdentifierAsync(entry.CardIdentifier, ct);
            var marketValue = card?.CurrentMarketValue ?? 0;
            totalValue += marketValue;
            entriesWithValue.Add(new
            {
                entry.Id,
                CardIdentifier = entry.CardIdentifier.ToUpperInvariant(),
                CardName = card?.Name,
                MarketValue = marketValue,
                entry.CreatedAt
            });
        }

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
