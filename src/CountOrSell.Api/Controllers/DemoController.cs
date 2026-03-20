using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/demo")]
public class DemoController : ControllerBase
{
    private readonly IDemoModeService _demoModeService;

    public DemoController(IDemoModeService demoModeService)
    {
        _demoModeService = demoModeService;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        if (!_demoModeService.IsDemo)
            return NotFound();

        var visitorId = HttpContext.Session.GetString("visitor_id");
        if (string.IsNullOrEmpty(visitorId))
        {
            visitorId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("visitor_id", visitorId);
        }

        return Ok(new
        {
            isDemo = true,
            expiresAt = _demoModeService.ExpiresAt,
            secondsRemaining = _demoModeService.SecondsRemaining,
            visitorId,
            demoSets = _demoModeService.DemoSets,
        });
    }
}
