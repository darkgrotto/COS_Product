using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ICardRepository
{
    Task<Card?> GetByIdentifierAsync(string identifier, CancellationToken ct = default);
    Task<List<Card>> SearchByNameAsync(string query, CancellationToken ct = default);
    Task<Card> UpdateAsync(Card card, CancellationToken ct = default);
    Task<List<Card>> GetBySetCodeAsync(string setCode, CancellationToken ct = default);
}
