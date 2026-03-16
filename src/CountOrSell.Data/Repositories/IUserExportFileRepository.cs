using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface IUserExportFileRepository
{
    Task<UserExportFile> CreateAsync(UserExportFile exportFile, CancellationToken ct = default);
    Task<List<UserExportFile>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserExportFile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
