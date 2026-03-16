using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Domain.Services;

public interface IBackupService
{
    Task<BackupRecord> TakeBackupAsync(BackupType backupType, CancellationToken ct);
}
