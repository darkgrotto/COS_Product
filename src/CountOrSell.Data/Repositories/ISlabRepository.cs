using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ISlabRepository
{
    Task<SlabEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SlabEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<SlabEntry>> GetByUserFilteredAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default);
    Task<SlabEntry> CreateAsync(SlabEntry entry, CancellationToken ct = default);
    Task<SlabEntry> UpdateAsync(SlabEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> BulkDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default);
    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountByAgencyCodeAsync(string agencyCode, CancellationToken ct = default);
    Task RemapAgencyCodeAsync(string oldCode, string newCode, CancellationToken ct = default);
}
