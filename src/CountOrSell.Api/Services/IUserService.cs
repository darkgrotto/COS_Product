using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Api.Services;

public interface IUserService
{
    Task<UserServiceResult> CreateLocalUserAsync(string username, string displayName, string password, UserRole role, CancellationToken ct = default);
    Task<UserServiceResult> DisableUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> RemoveUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> DemoteUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> PromoteUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> ReEnableUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<List<UserExportFile>> GetExportFilesAsync(Guid userId, CancellationToken ct = default);
    Task<UserExportFile?> GetExportFileAsync(Guid exportFileId, CancellationToken ct = default);
    Task DeleteExportFileAsync(Guid exportFileId, CancellationToken ct = default);
    Task<UserPreferences?> GetPreferencesAsync(Guid userId, CancellationToken ct = default);
    Task UpdatePreferencesAsync(Guid userId, bool? setCompletionRegularOnly, string? defaultPage, CancellationToken ct = default);
}
