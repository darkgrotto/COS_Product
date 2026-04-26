using CountOrSell.Domain.Dtos.Signing;

namespace CountOrSell.Domain.Services;

// Fetches and caches the COS publishing JWKS. Performs RFC 7638 thumbprint
// validation against a hardcoded list of trusted keys (TOFU) before accepting
// a fetched JWKS. Falls back to a persisted local cache when the network is
// unavailable. If neither a verified live response nor a cached copy is
// available, lookups return null and the caller must refuse the update.
public interface IJwksProvider
{
    // Looks up a key by its kid. If not present in the in-memory cache, performs one
    // refresh attempt (in case a rotation just happened) and retries before giving up.
    Task<CosJwk?> GetKeyByKidAsync(string kid, CancellationToken ct);

    // Forces a live fetch + TOFU validation + cache write. Returns true on success.
    // A failed refresh leaves any existing cached JWKS untouched.
    Task<bool> RefreshAsync(CancellationToken ct);
}
