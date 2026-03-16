using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class PreUpdateBackupService : IPreUpdateBackupService
{
    private readonly ILogger<PreUpdateBackupService> _logger;

    public PreUpdateBackupService(ILogger<PreUpdateBackupService> logger) => _logger = logger;

    public Task<bool> TakeBackupAsync(string label, CancellationToken ct)
    {
        // Stub: backup service not yet implemented
        _logger.LogInformation("Pre-update backup stub called with label: {Label}", label);
        return Task.FromResult(true);
    }
}
