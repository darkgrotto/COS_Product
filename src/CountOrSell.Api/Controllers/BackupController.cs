using System.Security.Claims;
using CountOrSell.Api.Filters;
using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/backup")]
[Authorize(Roles = "Admin")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly IRestoreService _restoreService;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditLogger _audit;

    public BackupController(
        IBackupService backupService,
        IRestoreService restoreService,
        AppDbContext db,
        IConfiguration config,
        IAuditLogger audit)
    {
        _backupService = backupService;
        _restoreService = restoreService;
        _db = db;
        _config = config;
        _audit = audit;
    }

    private string ActorName => User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
    private string ActorDisplayName => User.FindFirstValue("display_name") ?? ActorName;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var lastScheduled = await _db.BackupRecords
            .Where(b => b.BackupType == BackupType.Scheduled)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.Label, b.CreatedAt, b.SchemaVersion })
            .FirstOrDefaultAsync(ct);

        var lastPreUpdate = await _db.BackupRecords
            .Where(b => b.BackupType == BackupType.PreUpdate)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.Label, b.CreatedAt, b.SchemaVersion })
            .FirstOrDefaultAsync(ct);

        var destinations = await _db.BackupDestinationConfigs
            .Where(d => d.IsActive)
            .Select(d => new { d.Id, Type = d.DestinationType, d.Label, d.IsActive })
            .ToListAsync(ct);

        var nextScheduled = CalculateNextScheduledTime();

        return Ok(new
        {
            lastScheduledBackup = lastScheduled,
            lastPreUpdateBackup = lastPreUpdate,
            nextScheduledBackup = nextScheduled,
            destinations
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.BackupRecords.OrderByDescending(b => b.CreatedAt);
        var total = await query.CountAsync(ct);
        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.Id,
                b.Label,
                b.BackupType,
                b.SchemaVersion,
                b.CreatedAt,
                b.FileSizeBytes,
                b.IsAvailable
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, records });
    }

    [HttpPost("trigger")]
    [DemoLocked]
    public async Task<IActionResult> TriggerBackup(CancellationToken ct)
    {
        var record = await _backupService.TakeBackupAsync(BackupType.Scheduled, ct);
        await _audit.LogAsync(ActorName, ActorDisplayName, "backup.trigger", record.Label, "success", ClientIp);
        return Ok(new { record.Id, record.Label, record.CreatedAt });
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadBackup(Guid id, CancellationToken ct)
    {
        var record = await _db.BackupRecords.FindAsync(new object[] { id }, ct);
        if (record == null) return NotFound(new { error = "Backup not found." });
        if (!record.IsAvailable) return Gone();

        var basePath = Path.Combine(AppContext.BaseDirectory, "backups");
        if (!BackupFileName.TryResolvePath(basePath, record, out var filePath))
            return NotFound(new { error = "Backup file not available for download from local storage." });

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, "application/zip", BackupFileName.For(record));
    }

    [HttpPost("destinations")]
    [DemoLocked]
    public async Task<IActionResult> AddDestination(
        [FromBody] AddDestinationRequest request,
        CancellationToken ct)
    {
        var dest = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = request.DestinationType,
            Label = request.Label,
            ConfigurationJson = request.ConfigurationJson,
            IsActive = true
        };
        _db.BackupDestinationConfigs.Add(dest);
        await _db.SaveChangesAsync(ct);
        return Ok(new { dest.Id, dest.DestinationType, dest.Label, dest.IsActive });
    }

    [HttpGet("destinations")]
    public async Task<IActionResult> GetDestinations(CancellationToken ct)
    {
        var destinations = await _db.BackupDestinationConfigs
            .Select(d => new { d.Id, d.DestinationType, d.Label, d.IsActive })
            .ToListAsync(ct);
        return Ok(destinations);
    }

    [HttpDelete("destinations/{id}")]
    [DemoLocked]
    public async Task<IActionResult> RemoveDestination(Guid id, CancellationToken ct)
    {
        var dest = await _db.BackupDestinationConfigs.FindAsync(new object[] { id }, ct);
        if (dest == null) return NotFound(new { error = "Destination not found." });
        _db.BackupDestinationConfigs.Remove(dest);
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("destinations/{id}/test")]
    public async Task<IActionResult> TestDestination(
        Guid id,
        [FromServices] IBackupDestinationFactory factory,
        CancellationToken ct)
    {
        var config = await _db.BackupDestinationConfigs.FindAsync(new object[] { id }, ct);
        if (config == null) return NotFound(new { error = "Destination not found." });
        var dest = factory.Create(config);
        var ok = await dest.TestConnectionAsync(ct);
        return Ok(new { success = ok });
    }

    private DateTime CalculateNextScheduledTime()
    {
        var schedule = _config["BACKUP_SCHEDULE"] ?? "weekly";
        var now = DateTime.UtcNow;
        if (schedule == "weekly")
        {
            var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0 && now.TimeOfDay >= TimeSpan.FromHours(3))
                daysUntilSunday = 7;
            return new DateTime(now.Year, now.Month, now.Day, 3, 0, 0, DateTimeKind.Utc)
                .AddDays(daysUntilSunday);
        }
        else if (schedule == "daily")
        {
            var todayRun = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0, DateTimeKind.Utc);
            if (todayRun <= now) todayRun = todayRun.AddDays(1);
            return todayRun;
        }
        return now.AddDays(7);
    }

    private ObjectResult Gone() =>
        StatusCode(StatusCodes.Status410Gone, new { error = "Backup is no longer available." });
}

[Route("api/restore")]
[ApiController]
[Authorize(Roles = "Admin")]
public class RestoreController : ControllerBase
{
    private readonly IRestoreService _restoreService;
    private readonly AppDbContext _db;

    public RestoreController(IRestoreService restoreService, AppDbContext db)
    {
        _restoreService = restoreService;
        _db = db;
    }

    [HttpPost]
    [DemoLocked]
    [RequestSizeLimit(524_288_000)] // 500 MB
    public async Task<IActionResult> RestoreFromUpload(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No backup file provided." });

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Backup file must be a .zip archive." });

        using var stream = file.OpenReadStream();
        RestoreResult result;
        try
        {
            result = await _restoreService.RestoreAsync(stream, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        if (!result.Success)
        {
            // Schema version mismatch returns 409
            if (result.ErrorMessage != null && result.ErrorMessage.StartsWith("Cannot restore:"))
                return Conflict(new { error = result.ErrorMessage });
            return UnprocessableEntity(new { error = result.ErrorMessage });
        }

        return Ok(new { restoredSchemaVersion = result.RestoredSchemaVersion });
    }

    [HttpPost("{backupId}")]
    [DemoLocked]
    public async Task<IActionResult> RestoreFromRecord(Guid backupId, CancellationToken ct)
    {
        var record = await _db.BackupRecords.FindAsync(new object[] { backupId }, ct);
        if (record == null) return NotFound(new { error = "Backup record not found." });
        if (!record.IsAvailable) return Conflict(new { error = "Backup is no longer available." });

        var basePath = Path.Combine(AppContext.BaseDirectory, "backups");
        if (!BackupFileName.TryResolvePath(basePath, record, out var filePath))
            return NotFound(new { error = "Backup file not found in local storage." });

        using var stream = System.IO.File.OpenRead(filePath);
        RestoreResult result;
        try
        {
            result = await _restoreService.RestoreAsync(stream, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        if (!result.Success)
        {
            if (result.ErrorMessage != null && result.ErrorMessage.StartsWith("Cannot restore:"))
                return Conflict(new { error = result.ErrorMessage });
            return UnprocessableEntity(new { error = result.ErrorMessage });
        }

        return Ok(new { restoredSchemaVersion = result.RestoredSchemaVersion });
    }
}

public class AddDestinationRequest
{
    public string DestinationType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = "{}";
}
