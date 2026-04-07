using BCrypt.Net;
using CountOrSell.Api.Auth;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IExportService _exportService;
    private readonly ICollectionRepository _collection;
    private readonly ISerializedRepository _serialized;
    private readonly ISlabRepository _slabs;
    private readonly ISealedInventoryRepository _sealedInventory;
    private readonly IWishlistRepository _wishlist;
    private readonly ILocalAuthService _localAuth;
    private readonly AppDbContext _db;

    public UserService(
        IUserRepository users,
        IExportService exportService,
        ICollectionRepository collection,
        ISerializedRepository serialized,
        ISlabRepository slabs,
        ISealedInventoryRepository sealedInventory,
        IWishlistRepository wishlist,
        ILocalAuthService localAuth,
        AppDbContext db)
    {
        _users = users;
        _exportService = exportService;
        _collection = collection;
        _serialized = serialized;
        _slabs = slabs;
        _sealedInventory = sealedInventory;
        _wishlist = wishlist;
        _localAuth = localAuth;
        _db = db;
    }

    public async Task<UserServiceResult> CreateLocalUserAsync(
        string username, string displayName, string password, UserRole role, CancellationToken ct = default)
    {
        if (password.Length < 15)
            return UserServiceResult.Fail("Password must be at least 15 characters.");

        var existing = await _users.GetByUsernameAsync(username, ct);
        if (existing != null)
            return UserServiceResult.Fail("Username is already taken.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = displayName,
            AuthType = AuthType.Local,
            Role = role,
            IsBuiltinAdmin = false,
            State = AccountState.Active,
            PasswordHash = _localAuth.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _users.CreateAsync(user, ct);
        return UserServiceResult.Ok();
    }

    public async Task<UserServiceResult> ResetPasswordAsync(Guid targetUserId, string newPassword, CancellationToken ct = default)
    {
        if (newPassword.Length < 15)
            return UserServiceResult.Fail("Password must be at least 15 characters.");

        var user = await _users.GetByIdAsync(targetUserId, ct);
        if (user is null) return UserServiceResult.Fail("User not found.");
        if (user.AuthType != AuthType.Local)
            return UserServiceResult.Fail("Password reset is only available for local accounts.");

        user.PasswordHash = _localAuth.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        return UserServiceResult.Ok();
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

        // Step 1: Export user data - must succeed before any deletion
        try
        {
            await _exportService.ExportUserDataAsync(targetUserId, user.Username, ct);
        }
        catch (Exception ex)
        {
            return UserServiceResult.Fail($"Export failed - user data has not been deleted. Reason: {ex.Message}");
        }

        // Step 2: Delete collection data (only after successful export)
        await _collection.DeleteAllByUserAsync(targetUserId, ct);
        await _serialized.DeleteAllByUserAsync(targetUserId, ct);
        await _slabs.DeleteAllByUserAsync(targetUserId, ct);
        await _sealedInventory.DeleteAllByUserAsync(targetUserId, ct);
        await _wishlist.DeleteAllByUserAsync(targetUserId, ct);

        // Step 3: Mark user as removed (only after successful data deletion)
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

    public Task<List<UserExportFile>> GetExportFilesAsync(Guid userId, CancellationToken ct = default) =>
        _exportService.GetExportFilesForUserAsync(userId, ct);

    public Task<UserExportFile?> GetExportFileAsync(Guid exportFileId, CancellationToken ct = default) =>
        _exportService.GetExportFileByIdAsync(exportFileId, ct);

    public Task DeleteExportFileAsync(Guid exportFileId, CancellationToken ct = default) =>
        _exportService.DeleteExportFileAsync(exportFileId, ct);

    public async Task<UserServiceResult> UpdateDisplayNameAsync(Guid targetUserId, string displayName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return UserServiceResult.Fail("Display name cannot be blank.");
        if (displayName.Length > 100)
            return UserServiceResult.Fail("Display name cannot exceed 100 characters.");

        var user = await _users.GetByIdAsync(targetUserId, ct);
        if (user is null) return UserServiceResult.Fail("User not found.");

        user.DisplayName = displayName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        return UserServiceResult.Ok();
    }

    public async Task<UserPreferences?> GetPreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public async Task UpdatePreferencesAsync(Guid userId, bool? setCompletionRegularOnly, string? defaultPage, CancellationToken ct = default)
    {
        var prefs = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (prefs == null)
        {
            prefs = new UserPreferences { UserId = userId };
            _db.UserPreferences.Add(prefs);
        }

        if (setCompletionRegularOnly.HasValue)
            prefs.SetCompletionRegularOnly = setCompletionRegularOnly.Value;
        if (defaultPage != null)
            prefs.DefaultPage = defaultPage;

        await _db.SaveChangesAsync(ct);
    }
}
