using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly ICardRepository _cards;
    private readonly ITcgPlayerService _tcgPlayer;

    public CardsController(ICardRepository cards, ITcgPlayerService tcgPlayer)
    {
        _cards = cards;
        _tcgPlayer = tcgPlayer;
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
            card.Color,
            card.CardType,
            card.OracleRulingUrl,
            card.CurrentMarketValue,
            card.UpdatedAt,
            card.IsReserved
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Search query must be at least 2 characters." });

        var cards = await _cards.SearchByNameAsync(q, ct);
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
