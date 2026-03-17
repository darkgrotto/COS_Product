using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ICollectionRepository
{
    Task<List<ReservedCollectionEntry>> GetReservedEntriesForUserAsync(Guid userId, CancellationToken ct = default);
    Task<CollectionEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<CollectionEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<CollectionEntry>> GetByUserFilteredAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default);
    Task<CollectionEntry> CreateAsync(CollectionEntry entry, CancellationToken ct = default);
    Task<CollectionEntry> UpdateAsync(CollectionEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);
}
