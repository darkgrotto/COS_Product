using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/branding")]
[AllowAnonymous]
public class BrandingController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IDemoModeService _demoModeService;

    public BrandingController(IConfiguration config, IDemoModeService demoModeService)
    {
        _config = config;
        _demoModeService = demoModeService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var instanceName = _demoModeService.IsDemo
            ? "CountOrSell Demo"
            : (_config["INSTANCE_NAME"] ?? "CountOrSell");
        return Ok(new { instanceName });
    }
}
