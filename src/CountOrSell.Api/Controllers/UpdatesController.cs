using System.Security.Claims;
using CountOrSell.Api.Background.Updates;
using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/updates")]
[Authorize(Roles = "Admin")]
public class UpdatesController : ControllerBase
{
    private readonly IUpdateRepository _updateRepo;
    private readonly IUpdateCheckTrigger _updateTrigger;
    private readonly SchemaUpdateCoordinator _schemaCoordinator;

    public UpdatesController(
        IUpdateRepository updateRepo,
        IUpdateCheckTrigger updateTrigger,
        SchemaUpdateCoordinator schemaCoordinator)
    {
        _updateRepo = updateRepo;
        _updateTrigger = updateTrigger;
        _schemaCoordinator = schemaCoordinator;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var contentVersion = await _updateRepo.GetCurrentContentVersionAsync(ct);
        var pendingSchema = await _updateRepo.GetPendingSchemaUpdateAsync(ct);
        var latestAppVersion = await _updateRepo.GetLatestApplicationVersionAsync(ct);
        var appUpdatePending = latestAppVersion != null && latestAppVersion != ProductVersion.Current;

        return Ok(new
        {
            currentContentVersion = contentVersion,
            pendingSchemaUpdate = pendingSchema == null ? null : new
            {
                pendingSchema.Id,
                pendingSchema.SchemaVersion,
                pendingSchema.Description,
                pendingSchema.DetectedAt,
                pendingSchema.IsApproved,
                pendingSchema.ApprovedAt
            },
            latestApplicationVersion = latestAppVersion,
            applicationUpdatePending = appUpdatePending
        });
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        var notifications = await _updateRepo.GetUnreadNotificationsAsync(ct);
        return Ok(notifications.Select(n => new
        {
            n.Id, n.Message, n.Category, n.IsRead, n.CreatedAt
        }));
    }

    [HttpPost("check")]
    public async Task<IActionResult> TriggerCheck(CancellationToken ct)
    {
        await _updateTrigger.TriggerAsync(ct);
        return Ok();
    }

    [HttpPost("schema/{id}/approve")]
    public async Task<IActionResult> ApproveSchemaUpdate(int id, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var pending = await _updateRepo.GetPendingSchemaUpdateAsync(ct);
        if (pending == null || pending.Id != id)
            return NotFound(new { error = "Pending schema update not found." });

        // Record who approved it before executing
        pending.ApprovedByUserId = userId;

        var success = await _schemaCoordinator.ExecuteSchemaUpdateAsync(pending, ct);
        if (!success)
            return UnprocessableEntity(new
            {
                error = "Schema update could not be applied. " +
                        "Check admin notifications for details."
            });

        return Ok();
    }

    [HttpPost("notifications/{id}/read")]
    public async Task<IActionResult> MarkNotificationRead(int id, CancellationToken ct)
    {
        await _updateRepo.MarkNotificationReadAsync(id, ct);
        return Ok();
    }
}
