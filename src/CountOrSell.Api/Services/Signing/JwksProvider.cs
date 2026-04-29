using System.Text.Json;
using CountOrSell.Data;
using CountOrSell.Domain.Dtos.Signing;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services.Signing;

// Singleton JWKS provider with persistent cache (AppSettings DB rows) and TOFU validation.
//
// Lookup flow:
//   1. Serve from in-memory cache (loaded lazily from DB on first use).
//   2. On kid miss, perform one live refresh and retry.
//   3. If the refresh fails or fails TOFU, fall through to whatever was cached.
//   4. If nothing is cached at all, return null - caller MUST refuse the update.
//
// Refresh flow:
//   1. HTTP GET the JWKS URL.
//   2. Parse JSON; require non-empty keys[].
//   3. At least one key must have a thumbprint listed in TrustedKeyThumbprints.
//   4. Persist the raw JSON body to AppSettings and swap the in-memory cache.
//
// The raw JSON is stored, not a re-serialized form, so future lookups see the
// upstream byte-for-byte view (we never need to re-canonicalize for thumbprints).
public sealed class JwksProvider : IJwksProvider
{
    private const string JwksUrl = "https://www.countorsell.com/.well-known/cos-pubkey.json";
    private const string CacheBodyKey = "jwks_cache.body";
    private const string CacheFetchedAtKey = "jwks_cache.fetched_at";
    private const string HttpClientName = "Jwks";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JwksProvider> _logger;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    private CosJwks? _cached;
    private bool _cacheLoaded;

    public JwksProvider(
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<JwksProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<CosJwk?> GetKeyByKidAsync(string kid, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(kid)) return null;

        await EnsureCacheLoadedAsync(ct);

        var hit = LookupKid(_cached, kid);
        if (hit != null) return hit;

        // Cache miss - try one live refresh in case a rotation just happened.
        _logger.LogInformation("JWKS cache miss for kid {Kid}; refreshing", kid);
        await RefreshAsync(ct);
        return LookupKid(_cached, kid);
    }

    public async Task<bool> RefreshAsync(CancellationToken ct)
    {
        await _refreshGate.WaitAsync(ct);
        try
        {
            return await RefreshInternalAsync(ct);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task<bool> RefreshInternalAsync(CancellationToken ct)
    {
        string rawJson;
        try
        {
            var http = _httpClientFactory.CreateClient(HttpClientName);
            rawJson = await http.GetStringAsync(JwksUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch JWKS from {Url}; keeping cached copy", JwksUrl);
            return false;
        }

        CosJwks? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<CosJwks>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JWKS response is not valid JSON; keeping cached copy");
            return false;
        }

        if (parsed == null || parsed.Keys.Count == 0)
        {
            _logger.LogWarning("JWKS response had no keys; keeping cached copy");
            return false;
        }

        // TOFU: at least one key in the response must match a trusted thumbprint.
        var trusted = new HashSet<string>(TrustedKeyThumbprints.Values, StringComparer.Ordinal);
        var anyTrusted = parsed.Keys.Any(k => trusted.Contains(JwkThumbprint.Compute(k)));
        if (!anyTrusted)
        {
            _logger.LogWarning(
                "JWKS response did not contain any key matching a trusted thumbprint; refusing to adopt. " +
                "Continuing with cached JWKS if available.");
            return false;
        }

        _cached = parsed;
        await PersistAsync(rawJson, ct);
        _logger.LogInformation("JWKS refreshed and cached ({KeyCount} keys)", parsed.Keys.Count);
        return true;
    }

    private async Task EnsureCacheLoadedAsync(CancellationToken ct)
    {
        if (_cacheLoaded) return;
        await _refreshGate.WaitAsync(ct);
        try
        {
            if (_cacheLoaded) return;
            _cached = await LoadFromDbAsync(ct);
            _cacheLoaded = true;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task<CosJwks?> LoadFromDbAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var setting = await db.AppSettings.FindAsync(new object[] { CacheBodyKey }, ct);
            if (setting == null || string.IsNullOrEmpty(setting.Value)) return null;

            var jwks = JsonSerializer.Deserialize<CosJwks>(setting.Value, JsonOptions);
            if (jwks == null || jwks.Keys.Count == 0) return null;

            // Validate the on-disk cache too: if the cached body no longer matches any
            // trusted thumbprint (e.g. the trust list was tightened in a release),
            // discard it rather than silently using untrusted material.
            var trusted = new HashSet<string>(TrustedKeyThumbprints.Values, StringComparer.Ordinal);
            if (!jwks.Keys.Any(k => trusted.Contains(JwkThumbprint.Compute(k))))
            {
                _logger.LogWarning("Persisted JWKS no longer matches any trusted thumbprint; discarding");
                return null;
            }

            _logger.LogInformation("JWKS loaded from local cache ({KeyCount} keys)", jwks.Keys.Count);
            return jwks;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load JWKS from local cache");
            return null;
        }
    }

    private async Task PersistAsync(string rawJson, CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await UpsertAsync(db, CacheBodyKey, rawJson, ct);
            await UpsertAsync(db, CacheFetchedAtKey, DateTime.UtcNow.ToString("O"), ct);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist JWKS to local cache (in-memory copy still updated)");
        }
    }

    private static async Task UpsertAsync(AppDbContext db, string key, string value, CancellationToken ct)
    {
        var existing = await db.AppSettings.FindAsync(new object[] { key }, ct);
        if (existing == null)
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        else
            existing.Value = value;
    }

    private static CosJwk? LookupKid(CosJwks? jwks, string kid)
        => jwks?.Keys.FirstOrDefault(k => string.Equals(k.Kid, kid, StringComparison.Ordinal));
}
