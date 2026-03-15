using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface IWishlistRepository
{
    Task<List<WishlistEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<WishlistEntry> CreateAsync(WishlistEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
