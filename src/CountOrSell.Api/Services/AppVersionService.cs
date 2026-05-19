using CountOrSell.Domain.Services;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CountOrSell.Api.Services;

// Fetches the latest released application version from the GitHub Releases API.
// The release flow is "push a v{X}.{Y}.{Z} git tag" (see CLAUDE.md "Deployment"),
// so the latest release's tag_name is the authoritative source.
public class AppVersionService : IAppVersionService
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/darkgrotto/COS_Product/releases/latest";

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
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            // GitHub requires a User-Agent on every API request.
            request.Headers.UserAgent.ParseAdd("CountOrSell-AppVersionCheck");

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var release = JsonSerializer.Deserialize<GitHubReleaseResponse>(json, JsonOptions);
            var tag = release?.TagName;
            if (string.IsNullOrWhiteSpace(tag)) return null;

            // Tag format is "vX.Y.Z" - strip the leading "v" so consumers compare
            // against the application's own version string.
            return tag.StartsWith('v') || tag.StartsWith('V') ? tag[1..] : tag;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch latest application version");
            return null;
        }
    }

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
    }
}
