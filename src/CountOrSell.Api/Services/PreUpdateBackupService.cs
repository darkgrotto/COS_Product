using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class PreUpdateBackupService : IPreUpdateBackupService
{
    private readonly IBackupService _backup;
    private readonly ILogger<PreUpdateBackupService> _logger;

    public PreUpdateBackupService(
        IBackupService backup,
        ILogger<PreUpdateBackupService> logger)
    {
        _backup = backup;
        _logger = logger;
    }

    public async Task<bool> TakeBackupAsync(string label, CancellationToken ct)
    {
        try
        {
            await _backup.TakeBackupAsync(BackupType.PreUpdate, ct);
            _logger.LogInformation("Pre-update backup completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-update backup failed: {Message}", ex.Message);
            return false;
        }
    }
}
