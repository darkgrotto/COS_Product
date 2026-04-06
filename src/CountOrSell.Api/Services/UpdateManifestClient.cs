using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Services;
using System.Text.Json;

namespace CountOrSell.Api.Services;

public class UpdateManifestClient : IUpdateManifestClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateManifestClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UpdateManifestClient(HttpClient httpClient, ILogger<UpdateManifestClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UpdateManifest?> FetchManifestAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "https://www.countorsell.com/updates/manifest.json", ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch update manifest from countorsell.com");
            return null;
        }
    }

    public async Task<PackageManifest?> FetchPackageManifestAsync(string manifestUrl, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(manifestUrl, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<PackageManifest>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch per-package manifest from {Url}", manifestUrl);
            return null;
        }
    }
}
