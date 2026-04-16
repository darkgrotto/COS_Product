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
    private readonly IConfiguration _config;

    public AdminController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var userCount = await _db.Users.CountAsync(ct);
        var setCount = await _db.Sets.CountAsync(ct);
        var cardCount = await _db.Cards.CountAsync(ct);
        var sealedProductCount = await _db.SealedProducts.CountAsync(ct);
        var reservedListCount = await _db.Cards.CountAsync(c => c.IsReserved, ct);

        var imageStats = CountImages();

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

    private (int CardImages, int SealedImages) CountImages()
    {
        var basePath = _config["ImageStore:BasePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "images");
        if (!Directory.Exists(basePath)) return (0, 0);

        var setsPath = Path.Combine(basePath, "sets");
        var sealedPath = Path.Combine(basePath, "sealed");

        var cardImages = Directory.Exists(setsPath)
            ? Directory.GetFiles(setsPath, "*.jpg", SearchOption.AllDirectories).Length
            : 0;
        var sealedImages = Directory.Exists(sealedPath)
            ? Directory.GetFiles(sealedPath, "*.jpg", SearchOption.AllDirectories).Length
            : 0;

        return (cardImages, sealedImages);
    }
}
