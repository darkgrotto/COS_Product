using CountOrSell.Data;
using CountOrSell.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db) => _db = db;

    [HttpGet("backup")]
    public async Task<IActionResult> GetBackupSettings(CancellationToken ct)
    {
        var schedule = await GetSettingAsync("backup_schedule", "weekly", ct);
        var retentionScheduled = await GetSettingAsync("backup_retention_scheduled", "4", ct);
        var retentionPreUpdate = await GetSettingAsync("backup_retention_pre_update", "4", ct);

        return Ok(new
        {
            schedule,
            retentionScheduled = int.TryParse(retentionScheduled, out var rs) ? rs : 4,
            retentionPreUpdate = int.TryParse(retentionPreUpdate, out var rp) ? rp : 4
        });
    }

    [HttpPatch("backup")]
    public async Task<IActionResult> UpdateBackupSettings(
        [FromBody] BackupSettingsRequest request,
        CancellationToken ct)
    {
        if (request.Schedule != null)
            await UpsertSettingAsync("backup_schedule", request.Schedule, ct);

        if (request.RetentionScheduled.HasValue)
            await UpsertSettingAsync(
                "backup_retention_scheduled",
                request.RetentionScheduled.Value.ToString(),
                ct);

        if (request.RetentionPreUpdate.HasValue)
            await UpsertSettingAsync(
                "backup_retention_pre_update",
                request.RetentionPreUpdate.Value.ToString(),
                ct);

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    private async Task<string> GetSettingAsync(
        string key, string defaultValue, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { key }, ct);
        return setting?.Value ?? defaultValue;
    }

    private async Task UpsertSettingAsync(string key, string value, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { key }, ct);
        if (setting != null)
        {
            setting.Value = value;
        }
        else
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
    }
}

public class BackupSettingsRequest
{
    public string? Schedule { get; set; }
    public int? RetentionScheduled { get; set; }
    public int? RetentionPreUpdate { get; set; }
}
