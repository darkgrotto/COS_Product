using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ICollectionRepository
{
    Task<CollectionEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<CollectionEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<CollectionEntry> CreateAsync(CollectionEntry entry, CancellationToken ct = default);
    Task<CollectionEntry> UpdateAsync(CollectionEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
