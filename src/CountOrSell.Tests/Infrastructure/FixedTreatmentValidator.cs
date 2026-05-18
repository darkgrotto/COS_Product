using CountOrSell.Domain.Services;

namespace CountOrSell.Tests.Infrastructure;

// Validator returning a fixed set of treatment keys. Lets tests exercise the
// import services without standing up the IDbContextFactory the production
// TreatmentValidator depends on.
public sealed class FixedTreatmentValidator : ITreatmentValidator
{
    private readonly HashSet<string> _keys;

    public FixedTreatmentValidator(IEnumerable<string> keys)
    {
        _keys = new HashSet<string>(keys, StringComparer.Ordinal);
    }

    public Task<bool> IsValidAsync(string? key, CancellationToken ct = default) =>
        Task.FromResult(!string.IsNullOrEmpty(key) && _keys.Contains(key));

    public Task<IReadOnlyCollection<string>> GetValidKeysAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyCollection<string>>(_keys);

    public void Invalidate() { }
}
