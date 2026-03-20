using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/about")]
[Authorize]
public class AboutController : ControllerBase
{
    private readonly IUpdateRepository _updateRepo;
    private readonly IConfiguration _config;
    private readonly IDemoModeService _demoModeService;

    public AboutController(
        IUpdateRepository updateRepo,
        IConfiguration config,
        IDemoModeService demoModeService)
    {
        _updateRepo = updateRepo;
        _config = config;
        _demoModeService = demoModeService;
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
        return Ok(new
        {
            currentVersion = ProductVersion.Current,
            latestReleasedVersion = latestAppVersion ?? ProductVersion.Current,
            updatePending = isPending,
            lastContentUpdate = currentContentVersion,
            instanceName,
            isDemo = _demoModeService.IsDemo,
            demoSets = _demoModeService.DemoSets,
            license = new
            {
                name = "CC BY-NC-SA 4.0",
                fullName = "Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International",
                url = "https://creativecommons.org/licenses/by-nc-sa/4.0/"
            }
        });
    }
}
