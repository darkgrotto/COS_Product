using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CountOrSell.Api.Background.Updates;

public class UpdateCheckService : BackgroundService, IUpdateCheckTrigger
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UpdateCheckService> _logger;

    public UpdateCheckService(
        IConfiguration config,
        IServiceScopeFactory scopeFactory,
        ILogger<UpdateCheckService> logger)
    {
        _config = config;
        _scopeFactory = scopeFactory;
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
            await RunUpdateCheckAsync(stoppingToken);
        }
    }

    public async Task TriggerAsync(CancellationToken ct)
    {
        await RunUpdateCheckAsync(ct);
    }

    public async Task RunUpdateCheckAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var manifestClient = scope.ServiceProvider.GetRequiredService<IUpdateManifestClient>();
            var downloader = scope.ServiceProvider.GetRequiredService<IPackageDownloader>();
            var verifier = scope.ServiceProvider.GetRequiredService<IPackageVerifier>();
            var applicator = scope.ServiceProvider.GetRequiredService<IContentUpdateApplicator>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IAdminNotificationService>();
            var updateRepo = scope.ServiceProvider.GetRequiredService<IUpdateRepository>();

            var manifest = await manifestClient.FetchManifestAsync(ct);
            if (manifest == null) return;

            if (manifest.Packages.Count == 0)
            {
                _logger.LogInformation("No update packages available in manifest");
                return;
            }

            // Prefer the most recent package - last entry in the list
            var packageRef = manifest.Packages[^1];

            // Fetch per-package manifest to get checksums and content versions
            var packageManifest = await manifestClient.FetchPackageManifestAsync(packageRef.ManifestUrl, ct);
            if (packageManifest == null)
            {
                _logger.LogWarning("Could not fetch per-package manifest from {Url}", packageRef.ManifestUrl);
                return;
            }

            // Check current content version against what the package provides
            if (!packageManifest.ContentVersions.TryGetValue("cards", out var cardsVersion))
            {
                _logger.LogWarning("Package manifest missing cards content version");
                return;
            }

            var currentContentVersion = await updateRepo.GetCurrentContentVersionAsync(ct);
            if (currentContentVersion == cardsVersion.Version)
            {
                _logger.LogInformation("Content already up to date at version {Version}", currentContentVersion);
                return;
            }

            // Download the package ZIP
            var packageStream = await downloader.DownloadPackageAsync(packageRef.DownloadUrl, ct);

            // Apply the content update - the applicator verifies per-file checksums internally
            await applicator.ApplyContentUpdateAsync(packageStream, packageManifest, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update check failed");
        }
    }

    private TimeSpan CalculateDelayUntilNextRun()
    {
        var configTime = _config["UPDATE_CHECK_TIME"] ?? "02:00";
        if (!TimeOnly.TryParse(configTime, out var target))
            target = new TimeOnly(2, 0);
        var now = DateTime.UtcNow;
        var todayRun = new DateTime(now.Year, now.Month, now.Day, target.Hour, target.Minute, 0, DateTimeKind.Utc);
        if (todayRun <= now) todayRun = todayRun.AddDays(1);
        return todayRun - now;
    }
}
