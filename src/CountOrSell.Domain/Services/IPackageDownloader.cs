namespace CountOrSell.Domain.Services;

public interface IPackageDownloader
{
    Task<Stream> DownloadPackageAsync(string downloadUrl, CancellationToken ct);
}
