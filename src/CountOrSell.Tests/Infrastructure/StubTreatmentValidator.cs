using CountOrSell.Domain.Services;

namespace CountOrSell.Tests.Infrastructure;

// Stub validator that accepts any non-empty key. Tests that exercise
// ContentUpdateApplicator do not need real validation; this avoids the
// DbContextFactory dependency the production validator requires.
public sealed class StubTreatmentValidator : ITreatmentValidator
{
    public Task<bool> IsValidAsync(string? key, CancellationToken ct = default) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(key));

    public Task<IReadOnlyCollection<string>> GetValidKeysAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());

    public void Invalidate() { }
}
