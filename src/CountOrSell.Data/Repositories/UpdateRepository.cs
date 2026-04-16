using System.Text.Json;
using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class UpdateRepository : IUpdateRepository
{
    private readonly AppDbContext _db;

    public UpdateRepository(AppDbContext db) => _db = db;

    public async Task<string?> GetCurrentContentVersionAsync(CancellationToken ct)
    {
        var latest = await _db.UpdateVersions
            .OrderByDescending(u => u.AppliedAt)
            .FirstOrDefaultAsync(ct);
        return latest?.ContentVersion;
    }

    public async Task<int> GetCurrentSchemaVersionAsync(CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { "current_schema_version" }, ct);
        if (setting == null) return 1;
        return int.TryParse(setting.Value, out var v) ? v : 1;
    }

    public Task<PendingSchemaUpdate?> GetPendingSchemaUpdateAsync(CancellationToken ct) =>
        _db.PendingSchemaUpdates
            .Where(p => !p.IsApproved)
            .OrderByDescending(p => p.DetectedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddPendingSchemaUpdateAsync(PendingSchemaUpdate update, CancellationToken ct)
    {
        _db.PendingSchemaUpdates.Add(update);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ApprovePendingSchemaUpdateAsync(int id, Guid approvedByUserId, CancellationToken ct)
    {
        var pending = await _db.PendingSchemaUpdates.FindAsync(new object[] { id }, ct);
        if (pending == null) return;
        pending.IsApproved = true;
        pending.ApprovedAt = DateTime.UtcNow;
        pending.ApprovedByUserId = approvedByUserId;
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<AdminNotification>> GetUnreadNotificationsAsync(CancellationToken ct) =>
        _db.AdminNotifications
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task MarkNotificationReadAsync(int id, CancellationToken ct)
    {
        var notification = await _db.AdminNotifications.FindAsync(new object[] { id }, ct);
        if (notification == null) return;
        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllNotificationsReadAsync(CancellationToken ct)
    {
        var unread = await _db.AdminNotifications.Where(n => !n.IsRead).ToListAsync(ct);
        foreach (var n in unread)
            n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<string?> GetLatestApplicationVersionAsync(CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { "latest_app_version" }, ct);
        return setting?.Value;
    }

    public async Task SetLatestApplicationVersionAsync(string version, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { "latest_app_version" }, ct);
        if (setting == null)
        {
            _db.AppSettings.Add(new AppSetting { Key = "latest_app_version", Value = version });
        }
        else
        {
            setting.Value = version;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DateTime?> GetLastUpdateCheckedAtAsync(CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { "last_update_checked_at" }, ct);
        if (setting == null) return null;
        return DateTime.TryParse(setting.Value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : null;
    }

    public async Task SetLastUpdateCheckedAtAsync(DateTime checkedAt, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { "last_update_checked_at" }, ct);
        var value = checkedAt.ToUniversalTime().ToString("O");
        if (setting == null)
        {
            _db.AppSettings.Add(new AppSetting { Key = "last_update_checked_at", Value = value });
        }
        else
        {
            setting.Value = value;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Dictionary<string, ContentVersionEntry>?> GetComponentVersionsAsync(CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { "content_component_versions" }, ct);
        if (string.IsNullOrEmpty(setting?.Value)) return null;
        return JsonSerializer.Deserialize<Dictionary<string, ContentVersionEntry>>(setting.Value);
    }
}
