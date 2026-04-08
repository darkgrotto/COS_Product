using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ISealedInventoryRepository
{
    Task<SealedInventoryEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SealedInventoryEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<SealedInventoryEntry>> GetByUserFilteredAsync(Guid userId, string? categorySlug, string? subTypeSlug, CancellationToken ct = default);
    Task<SealedInventoryEntry> CreateAsync(SealedInventoryEntry entry, CancellationToken ct = default);
    Task<SealedInventoryEntry> UpdateAsync(SealedInventoryEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> BulkDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default);
    Task<int> BulkSetConditionAsync(IEnumerable<Guid> ids, Guid userId, string condition, CancellationToken ct = default);
    Task<int> BulkSetAcquisitionDateAsync(IEnumerable<Guid> ids, Guid userId, DateOnly date, CancellationToken ct = default);
    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);
}
