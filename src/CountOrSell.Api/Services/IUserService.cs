using CountOrSell.Domain.Models;

namespace CountOrSell.Api.Services;

public interface IUserService
{
    Task<UserServiceResult> DisableUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> RemoveUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> DemoteUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> PromoteUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> ReEnableUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<List<UserExportFile>> GetExportFilesAsync(Guid userId, CancellationToken ct = default);
    Task DeleteExportFileAsync(Guid exportFileId, CancellationToken ct = default);
    Task<UserPreferences?> GetPreferencesAsync(Guid userId, CancellationToken ct = default);
    Task UpdatePreferencesAsync(Guid userId, bool? setCompletionRegularOnly, string? defaultPage, CancellationToken ct = default);
}
