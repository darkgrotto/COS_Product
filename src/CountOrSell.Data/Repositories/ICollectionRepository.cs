using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ICollectionRepository
{
    Task<List<ReservedCollectionEntry>> GetReservedEntriesForUserAsync(Guid userId, CancellationToken ct = default);
    Task<CollectionEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<CollectionEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<CollectionEntry>> GetByUserFilteredAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default);
    Task<(List<CollectionEntry> Items, int Total)> GetByUserPagedAsync(Guid userId, CollectionFilter? filter, int page, int pageSize, CancellationToken ct = default);
    Task<CollectionEntry> CreateAsync(CollectionEntry entry, CancellationToken ct = default);
    Task BulkCreateAsync(List<CollectionEntry> entries, CancellationToken ct = default);
    Task<CollectionEntry> UpdateAsync(CollectionEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> BulkDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default);
    Task<int> BulkSetTreatmentAsync(IEnumerable<Guid> ids, Guid userId, string treatment, CancellationToken ct = default);
    Task<int> BulkSetAcquisitionDateAsync(IEnumerable<Guid> ids, Guid userId, DateOnly date, CancellationToken ct = default);
    Task<HashSet<string>> GetOwnedIdentifiersBySetAsync(Guid userId, string setCode, CancellationToken ct = default);
}
