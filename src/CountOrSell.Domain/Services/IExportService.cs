using CountOrSell.Domain.Models;

namespace CountOrSell.Domain.Services;

public interface IExportService
{
    Task<UserExportFile> ExportUserDataAsync(Guid userId, string username, CancellationToken ct = default);
    Task<List<UserExportFile>> GetExportFilesForUserAsync(Guid userId, CancellationToken ct = default);
    Task<UserExportFile?> GetExportFileByIdAsync(Guid exportFileId, CancellationToken ct = default);
    Task DeleteExportFileAsync(Guid exportFileId, CancellationToken ct = default);
}
