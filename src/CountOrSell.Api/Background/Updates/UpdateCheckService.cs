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

            // Select the most recent package by generated_at timestamp
            var packageRef = manifest.Packages.MaxBy(p => p.GeneratedAt) ?? manifest.Packages[^1];

            // Fetch per-package manifest to get checksums and content versions
            var packageManifest = await manifestClient.FetchPackageManifestAsync(packageRef.ManifestUrl, ct);
            if (packageManifest == null)
            {
                _logger.LogWarning("Could not fetch per-package manifest from {Url}", packageRef.ManifestUrl);
                result = new UpdateCheckResult(false, "Found a package but could not fetch its manifest.");
                return result;
            }

            // Use the package generated_at timestamp as the version key.
            // This detects updates to any content type (treatments, pricing, taxonomy, sealed
            // products, etc.), not just cards. The cards-only comparison previously caused updates
            // that contained non-card changes to be skipped with a false "already up to date".
            if (packageManifest.ContentVersions.Count == 0)
            {
                _logger.LogWarning("Package manifest has no content version entries");
                result = new UpdateCheckResult(false, "Package manifest is missing content version information.");
                return result;
            }

            var packageKey = packageManifest.GeneratedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var currentContentVersion = await updateRepo.GetCurrentContentVersionAsync(ct);
            if (!force && currentContentVersion == packageKey)
            {
                var displayDate = packageManifest.GeneratedAt.ToUniversalTime()
                    .ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);
                _logger.LogInformation("Content already up to date (package from {PackageDate})", displayDate);
                result = new UpdateCheckResult(false, $"Content is already up to date (last updated {displayDate}).");
                return result;
            }

            // Download the package ZIP
            var packageStream = await downloader.DownloadPackageAsync(packageRef.DownloadUrl, ct);

            // Apply the content update - the applicator verifies per-file checksums internally
            await applicator.ApplyContentUpdateAsync(packageStream, packageManifest, ct);

            var appliedDate = packageManifest.GeneratedAt.ToUniversalTime()
                .ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);
            result = new UpdateCheckResult(true, $"Content updated (package from {appliedDate}).");
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
