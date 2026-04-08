using System.Security.Claims;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly ICardRepository _cards;
    private readonly ITcgPlayerService _tcgPlayer;
    private readonly AppDbContext _db;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public CardsController(ICardRepository cards, ITcgPlayerService tcgPlayer, AppDbContext db)
    {
        _cards = cards;
        _tcgPlayer = tcgPlayer;
        _db = db;
    }

    // GET /api/cards/random-flavor
    // Returns a random card that has flavor text. Returns 204 if no cards with flavor text exist.
    [HttpGet("random-flavor")]
    public async Task<IActionResult> GetRandomFlavor(CancellationToken ct)
    {
        var card = await _cards.GetRandomWithFlavorTextAsync(ct);
        if (card == null) return NoContent();

        return Ok(new
        {
            Identifier = card.Identifier.ToUpperInvariant(),
            card.SetCode,
            card.Name,
            card.FlavorText,
            card.CurrentMarketValue
        });
    }

    [HttpGet("{identifier}")]
    public async Task<IActionResult> GetByIdentifier(string identifier, CancellationToken ct)
    {
        var card = await _cards.GetByIdentifierAsync(identifier.ToLowerInvariant(), ct);
        if (card == null) return NotFound();

        return Ok(new
        {
            Identifier = card.Identifier.ToUpperInvariant(),
            card.SetCode,
            card.Name,
            card.ManaCost,
            card.Cmc,
            card.Color,
            card.ColorIdentity,
            card.Keywords,
            card.CardType,
            card.OracleText,
            card.Layout,
            card.Rarity,
            card.OracleRulingUrl,
            card.FlavorText,
            card.CurrentMarketValue,
            card.UpdatedAt,
            card.IsReserved
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string? setCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Search query must be at least 2 characters." });

        var cards = await _cards.SearchAsync(q, setCode, ct);
        return Ok(cards.Select(c => new
        {
            Identifier = c.Identifier.ToUpperInvariant(),
            c.SetCode,
            c.Name,
            c.Color,
            c.CardType,
            c.CurrentMarketValue,
            c.IsReserved
        }));
    }

    [HttpGet("{identifier}/market-value")]
    public async Task<IActionResult> GetMarketValue(string identifier, CancellationToken ct)
    {
        var card = await _cards.GetByIdentifierAsync(identifier.ToLowerInvariant(), ct);
        if (card == null) return NotFound();

        return Ok(new
        {
            Identifier = card.Identifier.ToUpperInvariant(),
            card.CurrentMarketValue,
            card.UpdatedAt
        });
    }

    // GET /api/cards/reserved-list
    // Returns all Reserved List cards with the current user's owned quantity (0 if not owned).
    [HttpGet("reserved-list")]
    public async Task<IActionResult> GetReservedList(CancellationToken ct)
    {
        var userId = CurrentUserId;

        var reservedCards = await _db.Cards
            .Where(c => c.IsReserved)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.SetCode)
            .Select(c => new
            {
                Identifier = c.Identifier.ToUpperInvariant(),
                c.SetCode,
                c.Name,
                c.CardType,
                c.Color,
                c.CurrentMarketValue,
            })
            .ToListAsync(ct);

        var identifiers = reservedCards.Select(c => c.Identifier.ToLowerInvariant()).ToList();

        var owned = await _db.CollectionEntries
            .Where(e => e.UserId == userId && identifiers.Contains(e.CardIdentifier))
            .GroupBy(e => e.CardIdentifier)
            .Select(g => new { Identifier = g.Key, Total = g.Sum(e => e.Quantity) })
            .ToDictionaryAsync(x => x.Identifier, x => x.Total, ct);

        return Ok(reservedCards.Select(c => new
        {
            c.Identifier,
            c.SetCode,
            c.Name,
            c.CardType,
            c.Color,
            c.CurrentMarketValue,
            OwnedQuantity = owned.TryGetValue(c.Identifier.ToLowerInvariant(), out var qty) ? qty : 0,
        }));
    }

    // GET /api/cards/reserved-identifiers
    // Returns identifiers of all reserved list cards (for badge rendering in collection views).
    // Lightweight - identifiers only.
    [HttpGet("reserved-identifiers")]
    public async Task<IActionResult> GetReservedIdentifiers(CancellationToken ct)
    {
        var identifiers = await _cards.GetReservedIdentifiersAsync(ct);
        return Ok(identifiers);
    }

    [HttpPost("{identifier}/refresh-price")]
    public async Task<IActionResult> RefreshPrice(string identifier, CancellationToken ct)
    {
        if (!_tcgPlayer.IsConfigured)
            return BadRequest(new { error = "TCGPlayer API key is not configured." });

        var card = await _cards.GetByIdentifierAsync(identifier.ToLowerInvariant(), ct);
        if (card == null) return NotFound();

        var price = await _tcgPlayer.GetMarketValueAsync(identifier.ToLowerInvariant(), ct);
        if (price == null)
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "TCGPlayer query returned no price." });

        card.CurrentMarketValue = price;
        card.UpdatedAt = DateTime.UtcNow;
        await _cards.UpdateAsync(card, ct);

        return Ok(new
        {
            Identifier = card.Identifier.ToUpperInvariant(),
            card.CurrentMarketValue,
            card.UpdatedAt
        });
    }
}
