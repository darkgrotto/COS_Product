using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/about")]
[Authorize]
public class AboutController : ControllerBase
{
    private readonly IUpdateRepository _updateRepo;
    private readonly IConfiguration _config;
    private readonly IDemoModeService _demoModeService;
    private readonly AppDbContext _db;

    public AboutController(
        IUpdateRepository updateRepo,
        IConfiguration config,
        IDemoModeService demoModeService,
        AppDbContext db)
    {
        _updateRepo = updateRepo;
        _config = config;
        _demoModeService = demoModeService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAbout(CancellationToken ct)
    {
        var instanceName = _demoModeService.IsDemo
            ? "CountOrSell Demo"
            : (_config["INSTANCE_NAME"] ?? "CountOrSell");

        var currentContentVersion = await _updateRepo.GetCurrentContentVersionAsync(ct);
        var latestAppVersion = await _updateRepo.GetLatestApplicationVersionAsync(ct);
        var isPending = latestAppVersion != null && latestAppVersion != ProductVersion.Current;
        var lastCheckedAt = await _updateRepo.GetLastUpdateCheckedAtAsync(ct);

        var totalCards = await _db.Cards.CountAsync(ct);
        var totalSets = await _db.Sets.CountAsync(ct);
        var imageStats = CountImages();

        return Ok(new
        {
            currentVersion = ProductVersion.Display,
            latestReleasedVersion = latestAppVersion ?? ProductVersion.Current,
            updatePending = isPending,
            lastContentUpdate = currentContentVersion,
            lastUpdateCheckedAt = lastCheckedAt,
            scheduledUpdateCheckTime = _config["UPDATE_CHECK_TIME"],
            instanceName,
            isDemo = _demoModeService.IsDemo,
            demoSets = _demoModeService.DemoSets,
            contentStats = new
            {
                totalCards,
                totalSets,
                totalCardImages = imageStats.CardImages,
                totalSealedImages = imageStats.SealedImages,
            },
            license = new
            {
                name = "CC BY-NC-SA 4.0",
                fullName = "Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International",
                url = "https://creativecommons.org/licenses/by-nc-sa/4.0/"
            }
        });
    }

    private (int CardImages, int SealedImages) CountImages()
    {
        var basePath = _config["ImageStore:BasePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "images");
        if (!Directory.Exists(basePath))
            return (0, 0);

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
