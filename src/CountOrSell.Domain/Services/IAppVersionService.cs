namespace CountOrSell.Domain.Services;

public interface IAppVersionService
{
    Task<string?> FetchLatestVersionAsync(CancellationToken ct);
}
