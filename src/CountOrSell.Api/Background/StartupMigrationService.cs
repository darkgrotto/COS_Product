using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Background;

public class StartupMigrationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StartupMigrationService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public StartupMigrationService(
        IServiceScopeFactory scopeFactory,
        ILogger<StartupMigrationService> logger,
        IHostApplicationLifetime lifetime)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _lifetime = lifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var preBackup = scope.ServiceProvider.GetRequiredService<IPreUpdateBackupService>();
        var restore = scope.ServiceProvider.GetRequiredService<IRestoreService>();
        var notifications = scope.ServiceProvider.GetRequiredService<IAdminNotificationService>();

        try
        {
            // Only attempt migration check for relational databases
            if (db.Database.IsRelational())
            {
                var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (!pendingMigrations.Any())
                {
                    _logger.LogInformation("No pending database migrations");
                    return;
                }

                var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
                var isFreshDatabase = !appliedMigrations.Any();

                if (isFreshDatabase)
                {
                    _logger.LogInformation(
                        "Found {Count} pending migrations on a fresh database, skipping pre-update backup",
                        pendingMigrations.Count);
                }
                else
                {
                    _logger.LogInformation(
                        "Found {Count} pending migrations, taking pre-update backup...",
                        pendingMigrations.Count);

                    // Use CancellationToken.None: backup runs pg_dump which can take longer
                    // than the host startup timeout. The startup token must not cancel it.
                    var backupOk = await preBackup.TakeBackupAsync("startup-migration", CancellationToken.None);
                    if (!backupOk)
                    {
                        _logger.LogError("Startup migration aborted: pre-update backup failed");
                        try
                        {
                            await notifications.NotifyAsync(
                                "Startup migration aborted: pre-update backup failed. " +
                                "Fix backup configuration and restart.",
                                "schema", CancellationToken.None);
                        }
                        catch (Exception notifyEx)
                        {
                            _logger.LogError(notifyEx, "Failed to send notification after backup failure");
                        }
                        _lifetime.StopApplication();
                        return;
                    }
                }

                try
                {
                    // Use CancellationToken.None for the same reason: migration must complete
                    // or explicitly fail; it must not be abandoned mid-flight by a timeout.
                    await db.Database.MigrateAsync(CancellationToken.None);
                    _logger.LogInformation("Startup migrations applied successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Startup migration failed, attempting restore from backup");

                    BackupRecord? latestBackup = null;
                    if (!isFreshDatabase)
                    {
                        latestBackup = await db.BackupRecords
                            .Where(b => b.BackupType == BackupType.PreUpdate)
                            .OrderByDescending(b => b.CreatedAt)
                            .FirstOrDefaultAsync(CancellationToken.None);
                    }

                    if (latestBackup != null)
                    {
                        var backupPath = Path.Combine(
                            AppContext.BaseDirectory,
                            "backups",
                            $"{latestBackup.Label}.zip");

                        if (File.Exists(backupPath))
                        {
                            try
                            {
                                using var stream = File.OpenRead(backupPath);
                                await restore.RestoreAsync(stream, CancellationToken.None);
                                _logger.LogInformation("Restore from pre-update backup succeeded");
                            }
                            catch (Exception restoreEx)
                            {
                                _logger.LogError(restoreEx,
                                    "Restore also failed - manual intervention required");
                            }
                        }
                    }

                    if (!isFreshDatabase)
                    {
                        try
                        {
                            await notifications.NotifyAsync(
                                $"Startup migration failed: {ex.Message}. Application will not start.",
                                "schema", CancellationToken.None);
                        }
                        catch (Exception notifyEx)
                        {
                            _logger.LogError(notifyEx, "Failed to send notification after migration failure");
                        }
                    }
                    _lifetime.StopApplication();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup migration check failed unexpectedly");
            // Do not abort startup for unexpected errors - let the app try to start
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
