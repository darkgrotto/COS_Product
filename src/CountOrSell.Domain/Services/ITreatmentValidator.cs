namespace CountOrSell.Domain.Services;

public interface ITreatmentValidator
{
    Task<bool> IsValidAsync(string? key, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetValidKeysAsync(CancellationToken ct = default);
    void Invalidate();
}
