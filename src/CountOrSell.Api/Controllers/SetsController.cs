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
                s.ReleaseDate,
            })
            .ToListAsync(ct);

        return Ok(sets);
    }
}
