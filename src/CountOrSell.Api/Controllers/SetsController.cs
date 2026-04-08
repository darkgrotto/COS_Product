using CountOrSell.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/sets")]
[Authorize]
public class SetsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SetsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var sets = await _db.Sets
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                Code = s.Code.ToUpper(),
                s.Name,
                s.TotalCards,
                s.SetType,
                s.ReleaseDate,
            })
            .ToListAsync(ct);

        return Ok(sets);
    }

    [HttpGet("{setCode}/cards")]
    public async Task<IActionResult> GetCardsBySet(string setCode, CancellationToken ct)
    {
        var code = setCode.ToLowerInvariant();
        var exists = await _db.Sets.AnyAsync(s => s.Code == code, ct);
        if (!exists) return NotFound();

        var cards = await _db.Cards
            .Where(c => c.SetCode == code)
            .OrderBy(c => c.Identifier)
            .Select(c => new
            {
                Identifier = c.Identifier.ToUpperInvariant(),
                c.Name,
                c.Color,
                c.CardType,
                c.Rarity,
                c.CurrentMarketValue,
                c.IsReserved,
            })
            .ToListAsync(ct);

        return Ok(cards);
    }
}
