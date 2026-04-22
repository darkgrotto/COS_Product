using System.Security.Claims;
using CountOrSell.Api.Background.Updates;
using CountOrSell.Api.Filters;
using CountOrSell.Api.Services;
using CountOrSell.Api.Services.Deployment;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/updates")]
[Authorize(Roles = "Admin")]
public class UpdatesController : ControllerBase
{
    private readonly IUpdateRepository _updateRepo;
    private readonly IUpdateCheckTrigger _updateTrigger;
    private readonly SchemaUpdateCoordinator _schemaCoordinator;
    private readonly ICloudDeploymentService _deploymentService;
    private readonly IAuditLogger _audit;
    private readonly IServiceScopeFactory _scopeFactory;

    public UpdatesController(
        IUpdateRepository updateRepo,
        IUpdateCheckTrigger updateTrigger,
        SchemaUpdateCoordinator schemaCoordinator,
        ICloudDeploymentService deploymentService,
        IAuditLogger audit,
        IServiceScopeFactory scopeFactory)
    {
        _updateRepo = updateRepo;
        _updateTrigger = updateTrigger;
        _schemaCoordinator = schemaCoordinator;
        _deploymentService = deploymentService;
        _audit = audit;
        _scopeFactory = scopeFactory;
    }

    private string ActorName => User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
    private string ActorDisplayName => User.FindFirstValue("display_name") ?? ActorName;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var contentVersion = await _updateRepo.GetCurrentContentVersionAsync(ct);
        var pendingSchema = await _updateRepo.GetPendingSchemaUpdateAsync(ct);
        var latestAppVersion = await _updateRepo.GetLatestApplicationVersionAsync(ct);
        var appUpdatePending = latestAppVersion != null && latestAppVersion != ProductVersion.Current;
        var componentVersions = await _updateRepo.GetComponentVersionsAsync(ct);

        return Ok(new
        {
            currentContentVersion = contentVersion,
            componentVersions = componentVersions == null ? null :
                componentVersions.ToDictionary(
                    kv => kv.Key,
                    kv => new { version = kv.Value.Version, recordCount = kv.Value.RecordCount }),
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
    [DemoLocked]
    public async Task<IActionResult> TriggerCheck(CancellationToken ct)
    {
        var result = await _updateTrigger.TriggerAsync(ct);
        await _audit.LogAsync(ActorName, ActorDisplayName, "update.check", null,
            result.Message, ClientIp);
        return Ok(new { result.PackagesAvailable, result.Message });
    }

    [HttpPost("schema/{id}/approve")]
    [DemoLocked]
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
        {
            await _audit.LogAsync(ActorName, ActorDisplayName, "schema.approve",
                $"schema {pending.SchemaVersion}", "failed", ClientIp);
            return UnprocessableEntity(new
            {
                error = "Schema update could not be applied. " +
                        "Check admin notifications for details."
            });
        }

        await _audit.LogAsync(ActorName, ActorDisplayName, "schema.approve",
            $"schema {pending.SchemaVersion}", "success", ClientIp);
        return Ok();
    }

    [HttpGet("deploy/supported")]
    public IActionResult GetDeploySupported()
        => Ok(new { supported = _deploymentService.IsSupported });

    [HttpPost("deploy")]
    [DemoLocked]
    public async Task<IActionResult> TriggerDeploy([FromBody] DeployRequest? request, CancellationToken ct)
    {
        if (!_deploymentService.IsSupported)
            return UnprocessableEntity(new
            {
                error = "Application updates for this deployment type are managed via the generated update.sh script."
            });

        var result = await _deploymentService.TriggerUpdateAsync(request?.Tag, ct);
        if (!result.Success)
            return UnprocessableEntity(new { error = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("notifications/{id}/read")]
    public async Task<IActionResult> MarkNotificationRead(int id, CancellationToken ct)
    {
        await _updateRepo.MarkNotificationReadAsync(id, ct);
        return Ok();
    }

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken ct)
    {
        await _updateRepo.MarkAllNotificationsReadAsync(ct);
        return Ok();
    }

    [HttpPost("redownload")]
    [DemoLocked]
    public IActionResult ForceRedownload()
        => StartBackgroundRedownload(_updateTrigger.TriggerForceAsync, "update.redownload");

    [HttpPost("redownload-full")]
    [DemoLocked]
    public IActionResult ForceRedownloadFull()
        => StartBackgroundRedownload(_updateTrigger.TriggerForceFullAsync, "update.redownload.full");

    [HttpPost("redownload-targeted")]
    [DemoLocked]
    public IActionResult ForceRedownloadTargeted([FromBody] TargetedRedownloadRequest request)
    {
        var contentType = request.ContentType switch
        {
            "metadata" => "metadata",
            "images" => "images",
            _ => "all"
        };
        var scope = request.Scope switch
        {
            "cards-sets" => "cards-sets",
            "sealed" => "sealed",
            _ => "all"
        };

        var options = new RedownloadOptions(contentType, scope, request.UseFullPackage);
        var actorName = ActorName;
        var actorDisplayName = ActorDisplayName;
        var clientIp = ClientIp;
        var actionTarget = $"contentType={contentType},scope={scope},fullPackage={request.UseFullPackage}";

        _ = Task.Run(async () =>
        {
            var result = await _updateTrigger.TriggerTargetedRedownloadAsync(options, CancellationToken.None);
            await using var bgScope = _scopeFactory.CreateAsyncScope();
            var audit = bgScope.ServiceProvider.GetRequiredService<IAuditLogger>();
            await audit.LogAsync(actorName, actorDisplayName, "update.redownload.targeted",
                actionTarget, result.Message, clientIp);
        });

        return Accepted(new { message = "Targeted redownload started in background." });
    }

    private IActionResult StartBackgroundRedownload(
        Func<CancellationToken, Task<UpdateCheckResult>> trigger,
        string actionType)
    {
        var actorName = ActorName;
        var actorDisplayName = ActorDisplayName;
        var clientIp = ClientIp;

        // Run in background so the HTTP request is not held open for the full download/apply.
        _ = Task.Run(async () =>
        {
            var result = await trigger(CancellationToken.None);
            await using var scope = _scopeFactory.CreateAsyncScope();
            var audit = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
            await audit.LogAsync(actorName, actorDisplayName, actionType, null,
                result.Message, clientIp);
        });

        return Accepted(new { message = "Redownload started in background." });
    }
}

public sealed class DeployRequest
{
    // Optional image tag to deploy (e.g. "dev", "1.2.3"). Null or omitted means
    // re-deploy the currently configured tag without changing it.
    public string? Tag { get; init; }
}

public sealed class TargetedRedownloadRequest
{
    // "all" | "metadata" | "images"
    public string ContentType { get; init; } = "all";
    // "all" | "cards-sets" | "sealed"
    public string Scope { get; init; } = "all";
    public bool UseFullPackage { get; init; } = false;
}
