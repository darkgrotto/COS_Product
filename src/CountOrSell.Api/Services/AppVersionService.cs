using CountOrSell.Domain.Services;
using System.Text.Json;

namespace CountOrSell.Api.Services;

// Fetches latest application version from countorsell.com.
// The exact source URL is TBD (open decision).
public class AppVersionService : IAppVersionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppVersionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AppVersionService(HttpClient httpClient, ILogger<AppVersionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> FetchLatestVersionAsync(CancellationToken ct)
    {
        try
        {
            // Source URL is TBD - open decision
            var response = await _httpClient.GetAsync(
                "https://www.countorsell.com/app-version.json", ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonSerializer.Deserialize<AppVersionResponse>(json, JsonOptions);
            return doc?.Version;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch latest application version");
            return null;
        }
    }

    private sealed class AppVersionResponse
    {
        public string? Version { get; set; }
    }
}
