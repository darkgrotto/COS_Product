using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Background.Updates;

// Refreshes the publishing JWKS once at startup, then every 24 hours. Refresh
// failures are non-fatal - JwksProvider falls back to its persisted cache, and
// signature verification refuses any update where no trusted key can be located.
public sealed class JwksRefreshService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JwksRefreshService> _logger;

    public JwksRefreshService(
        IServiceScopeFactory scopeFactory,
        ILogger<JwksRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var jwks = scope.ServiceProvider.GetRequiredService<IJwksProvider>();
                await jwks.RefreshAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "JWKS refresh tick failed");
            }

            try { await Task.Delay(RefreshInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
