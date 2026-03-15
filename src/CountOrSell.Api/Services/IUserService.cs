namespace CountOrSell.Api.Services;

public interface IUserService
{
    Task<UserServiceResult> DisableUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> RemoveUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> DemoteUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> PromoteUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task<UserServiceResult> ReEnableUserAsync(Guid targetUserId, CancellationToken ct = default);
}
