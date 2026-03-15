using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;

    public UserService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<UserServiceResult> DisableUserAsync(Guid targetUserId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(targetUserId, ct);
        if (user is null) return UserServiceResult.Fail("User not found.");

        if (user.IsBuiltinAdmin)
            return UserServiceResult.Fail("The built-in admin account cannot be disabled.");

        if (user.Role == UserRole.Admin && user.AuthType == AuthType.Local)
        {
            var remainingLocalAdmins = await _users.CountLocalAdminsExcludingAsync(targetUserId, ct);
            if (remainingLocalAdmins == 0)
                return UserServiceResult.Fail("Cannot disable the last local admin account.");
        }

        user.State = AccountState.Disabled;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        return UserServiceResult.Ok();
    }

    public async Task<UserServiceResult> RemoveUserAsync(Guid targetUserId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(targetUserId, ct);
        if (user is null) return UserServiceResult.Fail("User not found.");

        if (user.IsBuiltinAdmin)
            return UserServiceResult.Fail("The built-in admin account cannot be removed.");

        if (user.Role == UserRole.Admin && user.AuthType == AuthType.Local)
        {
            var remainingLocalAdmins = await _users.CountLocalAdminsExcludingAsync(targetUserId, ct);
            if (remainingLocalAdmins == 0)
                return UserServiceResult.Fail("Cannot remove the last local admin account.");
        }

        // Stub: export workflow to be implemented in Step 14
        user.State = AccountState.Removed;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        return UserServiceResult.Ok();
    }

    public async Task<UserServiceResult> DemoteUserAsync(Guid targetUserId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(targetUserId, ct);
        if (user is null) return UserServiceResult.Fail("User not found.");

        if (user.IsBuiltinAdmin)
            return UserServiceResult.Fail("The built-in admin account cannot be demoted.");

        if (user.Role == UserRole.Admin && user.AuthType == AuthType.Local)
        {
            var remainingLocalAdmins = await _users.CountLocalAdminsExcludingAsync(targetUserId, ct);
            if (remainingLocalAdmins == 0)
                return UserServiceResult.Fail("Cannot demote the last local admin account.");
        }

        user.Role = UserRole.GeneralUser;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        return UserServiceResult.Ok();
    }

    public async Task<UserServiceResult> PromoteUserAsync(Guid targetUserId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(targetUserId, ct);
        if (user is null) return UserServiceResult.Fail("User not found.");

        user.Role = UserRole.Admin;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        return UserServiceResult.Ok();
    }

    public async Task<UserServiceResult> ReEnableUserAsync(Guid targetUserId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(targetUserId, ct);
        if (user is null) return UserServiceResult.Fail("User not found.");

        if (user.State != AccountState.Disabled)
            return UserServiceResult.Fail("Account is not disabled.");

        user.State = AccountState.Active;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        return UserServiceResult.Ok();
    }
}
