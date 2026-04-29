using CountOrSell.Api.Services;
using CountOrSell.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IImageStatsService _imageStats;

    public AdminController(AppDbContext db, IImageStatsService imageStats)
    {
        _db = db;
        _imageStats = imageStats;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var userCount = await _db.Users.CountAsync(ct);
        var setCount = await _db.Sets.CountAsync(ct);
        var cardCount = await _db.Cards.CountAsync(ct);
        var sealedProductCount = await _db.SealedProducts.CountAsync(ct);
        var reservedListCount = await _db.Cards.CountAsync(c => c.IsReserved, ct);

        var imageStats = await _imageStats.GetCountsAsync(ct);

        return Ok(new
        {
            userCount,
            setCount,
            cardCount,
            cardImageCount = imageStats.CardImages,
            sealedProductCount,
            sealedImageCount = imageStats.SealedImages,
            reservedListCount
        });
    }
}
