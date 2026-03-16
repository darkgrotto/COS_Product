namespace CountOrSell.Domain.Services;

public interface ISchemaVersionService
{
    Task<int> GetCurrentSchemaVersionAsync(CancellationToken ct);
    Task SetSchemaVersionAsync(int version, CancellationToken ct);
    int GetApplicationSchemaVersion();
}
