using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ISlabRepository
{
    Task<SlabEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SlabEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<SlabEntry> CreateAsync(SlabEntry entry, CancellationToken ct = default);
    Task<SlabEntry> UpdateAsync(SlabEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
