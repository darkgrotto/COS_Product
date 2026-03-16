using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Background.Backup;

public class BackupScheduleService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<BackupScheduleService> _logger;

    public BackupScheduleService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<BackupScheduleService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRun();
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested) break;

            using var scope = _scopeFactory.CreateScope();
            var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
            try
            {
                await backup.TakeBackupAsync(BackupType.Scheduled, stoppingToken);
                _logger.LogInformation("Scheduled backup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled backup failed");
            }
        }
    }

    private TimeSpan CalculateDelayUntilNextRun()
    {
        var schedule = _config["BACKUP_SCHEDULE"] ?? "weekly";
        var now = DateTime.UtcNow;

        if (schedule == "weekly")
        {
            var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0 && now.TimeOfDay >= TimeSpan.FromHours(3))
                daysUntilSunday = 7;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0, DateTimeKind.Utc)
                .AddDays(daysUntilSunday);
            return nextRun - now;
        }
        else if (schedule == "daily")
        {
            var todayRun = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0, DateTimeKind.Utc);
            if (todayRun <= now) todayRun = todayRun.AddDays(1);
            return todayRun - now;
        }

        // Default fallback: 7 days
        return TimeSpan.FromDays(7);
    }
}
