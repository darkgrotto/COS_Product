using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ICardRepository
{
    Task<Card?> GetByIdentifierAsync(string identifier, CancellationToken ct = default);
    Task<List<Card>> SearchByNameAsync(string query, CancellationToken ct = default);
    Task<List<Card>> SearchAsync(string query, string? setCode = null, CancellationToken ct = default);
    Task<Card> UpdateAsync(Card card, CancellationToken ct = default);
    Task<List<Card>> GetBySetCodeAsync(string setCode, CancellationToken ct = default);
    Task<List<string>> GetReservedIdentifiersAsync(CancellationToken ct = default);
    Task<Dictionary<string, decimal?>> GetMarketValuesByIdentifiersAsync(IEnumerable<string> identifiers, CancellationToken ct = default);
    Task<Dictionary<string, CardSummary>> GetSummaryByIdentifiersAsync(IEnumerable<string> identifiers, CancellationToken ct = default);
    Task<Card?> GetRandomWithFlavorTextAsync(CancellationToken ct = default);
    Task<Dictionary<string, Dictionary<string, decimal?>>> GetPricesByIdentifiersAsync(IEnumerable<string> identifiers, CancellationToken ct = default);
}
