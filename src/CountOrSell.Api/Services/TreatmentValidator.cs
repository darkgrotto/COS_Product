using CountOrSell.Data;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

// Validates treatment keys against the canonical treatments reference table.
// Caches the set of valid keys in memory; cache is reset when content updates apply
// new treatments (callers must invoke Invalidate()).
public class TreatmentValidator : ITreatmentValidator
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private HashSet<string>? _cache;

    public TreatmentValidator(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<bool> IsValidAsync(string? key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var keys = await LoadAsync(ct);
        return keys.Contains(key);
    }

    public async Task<IReadOnlyCollection<string>> GetValidKeysAsync(CancellationToken ct = default) =>
        await LoadAsync(ct);

    public void Invalidate()
    {
        _lock.Wait();
        try { _cache = null; }
        finally { _lock.Release(); }
    }

    private async Task<HashSet<string>> LoadAsync(CancellationToken ct)
    {
        if (_cache != null) return _cache;
        await _lock.WaitAsync(ct);
        try
        {
            if (_cache != null) return _cache;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var keys = await db.Treatments.Select(t => t.Key).ToListAsync(ct);
            _cache = new HashSet<string>(keys, StringComparer.Ordinal);
            return _cache;
        }
        finally { _lock.Release(); }
    }
}
