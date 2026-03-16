using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public class SchemaUpdateCoordinator
{
    private readonly IPreUpdateBackupService _preBackup;
    private readonly IRestoreService _restore;
    private readonly IAdminNotificationService _notifications;
    private readonly ILogger<SchemaUpdateCoordinator> _logger;
    private readonly AppDbContext _db;

    public SchemaUpdateCoordinator(
        IPreUpdateBackupService preBackup,
        IRestoreService restore,
        IAdminNotificationService notifications,
        ILogger<SchemaUpdateCoordinator> logger,
        AppDbContext db)
    {
        _preBackup = preBackup;
        _restore = restore;
        _notifications = notifications;
        _logger = logger;
        _db = db;
    }

    public async Task<bool> ExecuteSchemaUpdateAsync(
        PendingSchemaUpdate pending,
        CancellationToken ct)
    {
        // Step 1: pre-update backup
        var backupOk = await _preBackup.TakeBackupAsync(
            $"pre-update-{pending.SchemaVersion}", ct);

        if (!backupOk)
        {
            await _notifications.NotifyAsync(
                $"Schema update to {pending.SchemaVersion} was blocked: pre-update backup failed. " +
                "Check backup destination configuration before retrying.",
                "schema", ct);
            return false;
        }

        // Step 2: capture the latest pre-update backup for potential rollback
        var latestPreUpdateBackup = await _db.BackupRecords
            .Where(b => b.BackupType == BackupType.PreUpdate)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(ct);

        // Step 3: run EF Core migrations
        try
        {
            await _db.Database.MigrateAsync(ct);
            _logger.LogInformation("Schema update to {Version} succeeded", pending.SchemaVersion);

            pending.IsApproved = true;
            pending.ApprovedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyAsync(
                $"Schema update to version {pending.SchemaVersion} completed successfully.",
                "schema", ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Schema migration to {Version} failed, attempting restore",
                pending.SchemaVersion);

            // Step 4: attempt restore from pre-update backup
            if (latestPreUpdateBackup != null)
            {
                var backupPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "backups",
                    $"{latestPreUpdateBackup.Label}.zip");

                if (File.Exists(backupPath))
                {
                    try
                    {
                        using var stream = File.OpenRead(backupPath);
                        var result = await _restore.RestoreAsync(stream, ct);
                        if (result.Success)
                        {
                            await _notifications.NotifyAsync(
                                $"Schema update to {pending.SchemaVersion} failed and was automatically " +
                                $"rolled back from pre-update backup. Error: {ex.Message}",
                                "schema", ct);
                        }
                        else
                        {
                            await _notifications.NotifyAsync(
                                $"Schema update to {pending.SchemaVersion} failed AND automatic rollback failed. " +
                                $"Manual intervention required. Migration error: {ex.Message}. " +
                                $"Rollback error: {result.ErrorMessage}",
                                "schema", ct);
                        }
                    }
                    catch (Exception restoreEx)
                    {
                        _logger.LogError(restoreEx, "Restore after migration failure also failed");
                        await _notifications.NotifyAsync(
                            $"CRITICAL: Schema update failed and rollback failed. Manual intervention required. " +
                            $"Migration error: {ex.Message}",
                            "schema", ct);
                    }
                }
                else
                {
                    await _notifications.NotifyAsync(
                        $"Schema update to {pending.SchemaVersion} failed. " +
                        $"Pre-update backup file not found for automatic rollback. " +
                        $"Manual intervention required. Error: {ex.Message}",
                        "schema", ct);
                }
            }
            else
            {
                await _notifications.NotifyAsync(
                    $"Schema update to {pending.SchemaVersion} failed. " +
                    $"No pre-update backup available for rollback. " +
                    $"Manual intervention required. Error: {ex.Message}",
                    "schema", ct);
            }

            return false;
        }
    }
}
