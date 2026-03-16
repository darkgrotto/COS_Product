using CountOrSell.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/treatments")]
[Authorize]
public class TreatmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TreatmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var treatments = await _db.Treatments
            .OrderBy(t => t.SortOrder)
            .Select(t => new { t.Key, t.DisplayName, t.SortOrder })
            .ToListAsync(ct);

        return Ok(treatments);
    }
}
