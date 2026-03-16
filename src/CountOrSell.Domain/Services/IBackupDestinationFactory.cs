using CountOrSell.Domain.Models;

namespace CountOrSell.Domain.Services;

public interface IBackupDestinationFactory
{
    IBackupDestination Create(BackupDestinationConfig config);
}
