using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface IWishlistRepository
{
    Task<List<WishlistEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<(WishlistEntry Entry, Card? Card)>> GetByUserWithCardsAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default);
    Task<WishlistEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WishlistEntry> CreateAsync(WishlistEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> BulkDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default);
    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);
}
