using CountOrSell.Data.Images;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CountOrSell.Api.Background.Updates;

public class UpdateCheckService : BackgroundService, IUpdateCheckTrigger
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IImageStore _imageStore;
    private readonly ILogger<UpdateCheckService> _logger;

    public UpdateCheckService(
        IConfiguration config,
        IServiceScopeFactory scopeFactory,
        IImageStore imageStore,
        ILogger<UpdateCheckService> logger)
    {
        _config = config;
        _scopeFactory = scopeFactory;
        _imageStore = imageStore;
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
            await RunUpdateCheckAsync(force: false, fullPackageOnly: false, stoppingToken);
        }
    }

    public async Task<UpdateCheckResult> TriggerAsync(CancellationToken ct)
        => await RunUpdateCheckAsync(force: false, fullPackageOnly: false, ct);

    public async Task<UpdateCheckResult> TriggerForceAsync(CancellationToken ct)
        => await RunUpdateCheckAsync(force: true, fullPackageOnly: false, ct);

    public async Task<UpdateCheckResult> TriggerForceFullAsync(CancellationToken ct)
        => await RunUpdateCheckAsync(force: true, fullPackageOnly: true, ct);

    public async Task<UpdateCheckResult> RunUpdateCheckAsync(bool force, bool fullPackageOnly, CancellationToken ct)
    {
        UpdateCheckResult? result = null;
        IUpdateRepository? updateRepo = null;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var manifestClient = scope.ServiceProvider.GetRequiredService<IUpdateManifestClient>();
            var downloader = scope.ServiceProvider.GetRequiredService<IPackageDownloader>();
            var verifier = scope.ServiceProvider.GetRequiredService<IPackageVerifier>();
            var sigVerifier = scope.ServiceProvider.GetRequiredService<IManifestSignatureVerifier>();
            var jwks = scope.ServiceProvider.GetRequiredService<IJwksProvider>();
            var applicator = scope.ServiceProvider.GetRequiredService<IContentUpdateApplicator>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IAdminNotificationService>();
            updateRepo = scope.ServiceProvider.GetRequiredService<IUpdateRepository>();

            // Daily refresh of the JWKS - best-effort, lookups will fall back to cache.
            await jwks.RefreshAsync(ct);

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

            // Select the target package - most recent overall, or most recent full if requested
            var candidates = fullPackageOnly
                ? manifest.Packages
                    .Where(p => string.Equals(p.PackageType, "full", StringComparison.OrdinalIgnoreCase))
                    .ToList()
                : manifest.Packages;

            if (candidates.Count == 0)
            {
                _logger.LogInformation("No full update packages available in manifest");
                result = new UpdateCheckResult(false, "No full update packages are available at this time.");
                return result;
            }

            var packageRef = candidates.MaxBy(p => p.GeneratedAt) ?? candidates[^1];

            // Fetch per-package manifest AND its detached signature.
            var signed = await manifestClient.FetchSignedPackageManifestAsync(packageRef.ManifestUrl, ct);
            if (signed == null)
            {
                _logger.LogWarning("Could not fetch per-package manifest from {Url}", packageRef.ManifestUrl);
                result = new UpdateCheckResult(false,
                    "Found a package but could not fetch its manifest or signature.");
                return result;
            }

            // Verify the detached signature BEFORE consuming any field from the manifest.
            // Refusal here is fatal for this run; nothing in the manifest can be trusted yet.
            var sigResult = await sigVerifier.VerifyAsync(signed.ManifestBytes, signed.Envelope, ct);
            if (!sigResult.IsValid)
            {
                _logger.LogError(
                    "Refusing update package {PackageId}: manifest signature verification failed - {Reason}",
                    packageRef.PackageId, sigResult.Message);
                result = new UpdateCheckResult(false,
                    $"Update refused: manifest signature verification failed ({sigResult.Message}).");
                return result;
            }

            var packageManifest = signed.Parsed;

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

            var packageKey = packageManifest.GeneratedAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var currentContentVersion = await updateRepo.GetCurrentContentVersionAsync(ct);
            if (!force && currentContentVersion == packageKey)
            {
                var displayDate = packageManifest.GeneratedAt.UtcDateTime
                    .ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);
                _logger.LogInformation("Content already up to date (package from {PackageDate})", displayDate);
                result = new UpdateCheckResult(false, $"Content is already up to date (last updated {displayDate}).");
                return result;
            }

            // Downgrade detection. A signed-but-older package is a replay attack: the upstream
            // has been compromised or someone is feeding us a stale-but-valid manifest. Refuse
            // unless the caller explicitly forced the run (manual redownload covers reinstalls).
            if (!force
                && !string.IsNullOrEmpty(currentContentVersion)
                && string.Compare(packageKey, currentContentVersion, StringComparison.Ordinal) < 0)
            {
                _logger.LogError(
                    "Refusing update package {PackageId}: package timestamp {Package} is older than installed content {Installed}",
                    packageRef.PackageId, packageKey, currentContentVersion);
                result = new UpdateCheckResult(false,
                    "Update refused: candidate package is older than the currently installed content.");
                return result;
            }

            // Download the package ZIP
            var packageStream = await downloader.DownloadPackageAsync(packageRef.DownloadUrl, ct);

            // Derive the base URL for fetching image blobs (directory above manifest.json)
            var lastSlash = packageRef.ManifestUrl.LastIndexOf('/');
            var packageBaseUrl = lastSlash >= 0
                ? packageRef.ManifestUrl[..(lastSlash + 1)]
                : packageRef.ManifestUrl;

            // Apply the content update. Use CancellationToken.None so that an HTTP request
            // timeout cancelling ct cannot interrupt the transaction mid-apply; the DB
            // operations must complete atomically regardless of the caller's lifetime.
            await applicator.ApplyContentUpdateAsync(packageStream, packageManifest, packageBaseUrl, CancellationToken.None);

            // Delta packages only contain images that changed since the base full package.
            // If the applied package has no images AND the image store is empty (e.g. fresh
            // install or wiped volume), fall back to the most recent full package for images.
            var packageHasImages = packageManifest.Checksums.Keys
                .Any(k => k.StartsWith("images/", StringComparison.OrdinalIgnoreCase));
            if (!packageHasImages && !await _imageStore.HasImagesAsync(CancellationToken.None))
            {
                var fullRef = manifest.Packages
                    .Where(p => string.Equals(p.PackageType, "full", StringComparison.OrdinalIgnoreCase))
                    .MaxBy(p => p.GeneratedAt);
                if (fullRef != null)
                {
                    _logger.LogInformation(
                        "Applied package has no images and image store is empty; fetching images from full package {PackageId}",
                        fullRef.PackageId);
                    var fullSigned = await manifestClient.FetchSignedPackageManifestAsync(
                        fullRef.ManifestUrl, CancellationToken.None);
                    if (fullSigned != null)
                    {
                        var fullSigResult = await sigVerifier.VerifyAsync(
                            fullSigned.ManifestBytes, fullSigned.Envelope, CancellationToken.None);
                        if (!fullSigResult.IsValid)
                        {
                            _logger.LogError(
                                "Refusing image-only fetch from package {PackageId}: signature verification failed - {Reason}",
                                fullRef.PackageId, fullSigResult.Message);
                        }
                        else
                        {
                            var fullLastSlash = fullRef.ManifestUrl.LastIndexOf('/');
                            var fullBaseUrl = fullLastSlash >= 0
                                ? fullRef.ManifestUrl[..(fullLastSlash + 1)]
                                : fullRef.ManifestUrl;
                            await applicator.ApplyImagesOnlyAsync(
                                fullBaseUrl, fullSigned.Parsed, CancellationToken.None);
                        }
                    }
                }
            }

            var appliedDate = packageManifest.GeneratedAt.UtcDateTime
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
            // Record the check time using a fresh scope - the original scope may be disposed
            // by this point if the try block exited via an exception.
            try
            {
                await using var finalScope = _scopeFactory.CreateAsyncScope();
                var finalRepo = finalScope.ServiceProvider.GetRequiredService<IUpdateRepository>();
                await finalRepo.SetLastUpdateCheckedAtAsync(DateTime.UtcNow, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record last update check time");
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
