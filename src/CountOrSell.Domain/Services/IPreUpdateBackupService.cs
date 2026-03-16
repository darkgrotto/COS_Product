namespace CountOrSell.Domain.Services;

public interface IPreUpdateBackupService
{
    Task<bool> TakeBackupAsync(string label, CancellationToken ct);
}
