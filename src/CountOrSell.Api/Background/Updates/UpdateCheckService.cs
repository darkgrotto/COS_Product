using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
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

            var currentSchemaVersion = await updateRepo.GetCurrentSchemaVersionAsync(ct);
            if (currentSchemaVersion < manifest.Content.MinimumProductSchemaVersion)
            {
                await notificationService.NotifyAsync(
                    "Schema update required before content update can be applied.",
                    "schema", ct);
                return;
            }

            if (manifest.Schema != null)
            {
                var existing = await updateRepo.GetPendingSchemaUpdateAsync(ct);
                if (existing == null || existing.SchemaVersion != manifest.Schema.Version)
                {
                    await updateRepo.AddPendingSchemaUpdateAsync(new PendingSchemaUpdate
                    {
                        SchemaVersion = manifest.Schema.Version,
                        Description = manifest.Schema.Description,
                        DownloadUrl = manifest.Schema.DownloadUrl,
                        ZipSha256 = manifest.Schema.ZipSha256,
                        DetectedAt = DateTime.UtcNow
                    }, ct);
                    await notificationService.NotifyAsync(
                        $"Schema update {manifest.Schema.Version} is available and requires admin approval.",
                        "schema", ct);
                }
            }

            var currentContentVersion = await updateRepo.GetCurrentContentVersionAsync(ct);
            if (currentContentVersion == manifest.Content.Version) return;

            var packageStream = await downloader.DownloadPackageAsync(manifest.Content.DownloadUrl, ct);
            if (!verifier.VerifyChecksum(packageStream, manifest.Content.ZipSha256))
            {
                _logger.LogError("Checksum mismatch for content update {Version}", manifest.Content.Version);
                await notificationService.NotifyAsync(
                    $"Content update {manifest.Content.Version} checksum verification failed.",
                    "update", ct);
                return;
            }

            await applicator.ApplyContentUpdateAsync(packageStream, manifest.Content.Version, ct);
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
