using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Dtos.Signing;
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

    public async Task<SignedPackageManifest?> FetchSignedPackageManifestAsync(
        string manifestUrl, CancellationToken ct)
    {
        byte[] manifestBytes;
        try
        {
            var response = await _httpClient.GetAsync(manifestUrl, ct);
            response.EnsureSuccessStatusCode();
            manifestBytes = await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch per-package manifest from {Url}", manifestUrl);
            return null;
        }

        var sigUrl = manifestUrl + ".sig";
        SignedManifestEnvelope? envelope;
        try
        {
            var sigResponse = await _httpClient.GetAsync(sigUrl, ct);
            sigResponse.EnsureSuccessStatusCode();
            var sigBody = await sigResponse.Content.ReadAsStringAsync(ct);
            envelope = JsonSerializer.Deserialize<SignedManifestEnvelope>(sigBody, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch detached signature from {Url}", sigUrl);
            return null;
        }

        if (envelope == null
            || string.IsNullOrEmpty(envelope.Alg)
            || string.IsNullOrEmpty(envelope.Kid)
            || string.IsNullOrEmpty(envelope.Sig))
        {
            _logger.LogWarning("Detached signature at {Url} is missing required fields", sigUrl);
            return null;
        }

        PackageManifest? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<PackageManifest>(manifestBytes, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Per-package manifest at {Url} is not valid JSON", manifestUrl);
            return null;
        }

        if (parsed == null) return null;
        return new SignedPackageManifest(manifestBytes, envelope, parsed);
    }
}
