using CountOrSell.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogService _auditLog;

    public AuditController(IAuditLogService auditLog)
    {
        _auditLog = auditLog;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int limit = 100,
        [FromQuery] string? actionType = null,
        CancellationToken ct = default)
    {
        if (limit < 1) limit = 1;
        if (limit > 500) limit = 500;

        var entries = await _auditLog.GetEntriesAsync(limit, actionType, ct);
        return Ok(entries.Select(e => new
        {
            e.Id,
            e.Timestamp,
            e.Actor,
            e.ActorDisplayName,
            e.ActionType,
            e.Target,
            e.Result,
            e.IpAddress
        }));
    }

    [HttpGet("logs/action-types")]
    public async Task<IActionResult> GetActionTypes(CancellationToken ct)
    {
        var types = await _auditLog.GetActionTypesAsync(ct);
        return Ok(types);
    }
}
