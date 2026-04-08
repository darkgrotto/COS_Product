using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ISealedProductRepository
{
    Task<SealedProduct?> GetByIdentifierAsync(string identifier, CancellationToken ct = default);
    Task<Dictionary<string, SealedProduct>> GetByIdentifiersAsync(IEnumerable<string> identifiers, CancellationToken ct = default);
    Task<List<SealedProduct>> SearchAsync(string query, CancellationToken ct = default);
}
