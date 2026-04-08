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
            await RunUpdateCheckAsync(force: false, stoppingToken);
        }
    }

    public async Task<UpdateCheckResult> TriggerAsync(CancellationToken ct)
        => await RunUpdateCheckAsync(force: false, ct);

    public async Task<UpdateCheckResult> TriggerForceAsync(CancellationToken ct)
        => await RunUpdateCheckAsync(force: true, ct);

    public async Task<UpdateCheckResult> RunUpdateCheckAsync(bool force, CancellationToken ct)
    {
        UpdateCheckResult? result = null;
        IUpdateRepository? updateRepo = null;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var manifestClient = scope.ServiceProvider.GetRequiredService<IUpdateManifestClient>();
            var downloader = scope.ServiceProvider.GetRequiredService<IPackageDownloader>();
            var verifier = scope.ServiceProvider.GetRequiredService<IPackageVerifier>();
            var applicator = scope.ServiceProvider.GetRequiredService<IContentUpdateApplicator>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IAdminNotificationService>();
            updateRepo = scope.ServiceProvider.GetRequiredService<IUpdateRepository>();

            var manifest = await manifestClient.FetchManifestAsync(ct);
            if (manifest == null)
            {
                result = new UpdateCheckResult(false, "Could not reach update server. Check network connectivity.");
                return result;
            }

            if (manifest.Packages.Count == 0)
            {
                _logger.LogInformation("No update packages available in manifest");
                result = new UpdateCheckResult(false, "No update packages are available at this time.");
                return result;
            }

            // Prefer the most recent package - last entry in the list
            var packageRef = manifest.Packages[^1];

            // Fetch per-package manifest to get checksums and content versions
            var packageManifest = await manifestClient.FetchPackageManifestAsync(packageRef.ManifestUrl, ct);
            if (packageManifest == null)
            {
                _logger.LogWarning("Could not fetch per-package manifest from {Url}", packageRef.ManifestUrl);
                result = new UpdateCheckResult(false, "Found a package but could not fetch its manifest.");
                return result;
            }

            // Check current content version against what the package provides
            if (!packageManifest.ContentVersions.TryGetValue("cards", out var cardsVersion))
            {
                _logger.LogWarning("Package manifest missing cards content version");
                result = new UpdateCheckResult(false, "Package manifest is missing content version information.");
                return result;
            }

            var currentContentVersion = await updateRepo.GetCurrentContentVersionAsync(ct);
            if (!force && currentContentVersion == cardsVersion.Version)
            {
                _logger.LogInformation("Content already up to date at version {Version}", currentContentVersion);
                result = new UpdateCheckResult(false, $"Content is already up to date (version {currentContentVersion}).");
                return result;
            }

            // Download the package ZIP
            var packageStream = await downloader.DownloadPackageAsync(packageRef.DownloadUrl, ct);

            // Apply the content update - the applicator verifies per-file checksums internally
            await applicator.ApplyContentUpdateAsync(packageStream, packageManifest, ct);

            result = new UpdateCheckResult(true, $"Content updated to version {cardsVersion.Version}.");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update check failed");
            result = new UpdateCheckResult(false, "Update check encountered an error. Check the application logs.");
            return result;
        }
        finally
        {
            // Record the check time after the check completes, regardless of outcome.
            if (updateRepo != null)
            {
                try { await updateRepo.SetLastUpdateCheckedAtAsync(DateTime.UtcNow, ct); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to record last update check time"); }
            }
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
