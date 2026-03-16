namespace CountOrSell.Domain.Services;

public interface ITcgPlayerService
{
    bool IsConfigured { get; }
    Task<decimal?> GetMarketValueAsync(string cardIdentifier, CancellationToken ct = default);
}
