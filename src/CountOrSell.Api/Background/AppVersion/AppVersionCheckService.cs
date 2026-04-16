using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Background.AppVersion;

public class AppVersionCheckService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppVersionCheckService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    public AppVersionCheckService(
        IServiceScopeFactory scopeFactory,
        ILogger<AppVersionCheckService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunVersionCheckAsync(stoppingToken);
            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunVersionCheckAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var versionService = scope.ServiceProvider.GetRequiredService<IAppVersionService>();
            var updateRepo = scope.ServiceProvider.GetRequiredService<IUpdateRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IAdminNotificationService>();

            var latestVersion = await versionService.FetchLatestVersionAsync(ct);
            if (latestVersion == null) return;

            var stored = await updateRepo.GetLatestApplicationVersionAsync(ct);
            if (stored != latestVersion)
            {
                await updateRepo.SetLatestApplicationVersionAsync(latestVersion, ct);
                if (latestVersion != ProductVersion.Current)
                {
                    await notificationService.NotifyAsync(
                        $"Application version {latestVersion} is available. Current version: {ProductVersion.Display}.",
                        "update", ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application version check failed");
        }
    }
}
