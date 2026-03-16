using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class PackageDownloader : IPackageDownloader
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PackageDownloader> _logger;

    public PackageDownloader(HttpClient httpClient, ILogger<PackageDownloader> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Stream> DownloadPackageAsync(string downloadUrl, CancellationToken ct)
    {
        _logger.LogInformation("Downloading update package from {Url}", downloadUrl);
        var response = await _httpClient.GetAsync(downloadUrl, ct);
        response.EnsureSuccessStatusCode();

        // Read fully into MemoryStream so it is seekable for checksum verification
        var ms = new MemoryStream();
        await response.Content.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }
}
