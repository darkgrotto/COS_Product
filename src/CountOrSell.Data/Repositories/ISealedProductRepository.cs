using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ISealedProductRepository
{
    Task<Dictionary<string, SealedProduct>> GetByIdentifiersAsync(IEnumerable<string> identifiers, CancellationToken ct = default);
}
