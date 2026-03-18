using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ISerializedRepository
{
    Task<SerializedEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SerializedEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<SerializedEntry>> GetByUserFilteredAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default);
    Task<SerializedEntry> CreateAsync(SerializedEntry entry, CancellationToken ct = default);
    Task<SerializedEntry> UpdateAsync(SerializedEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);
}
