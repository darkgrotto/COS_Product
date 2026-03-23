using System.Security.Cryptography;
using CountOrSell.Api.Auth;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public class InvitationService : IInvitationService
{
    private const int TokenExpiryHours = 72;
    private const int MinPasswordLength = 15;

    private readonly AppDbContext _db;
    private readonly IUserRepository _users;
    private readonly ILocalAuthService _localAuth;
    private readonly IEmailNotificationService _email;

    public InvitationService(
        AppDbContext db,
        IUserRepository users,
        ILocalAuthService localAuth,
        IEmailNotificationService email)
    {
        _db = db;
        _users = users;
        _localAuth = localAuth;
        _email = email;
    }

    public async Task<(UserInvitation Invitation, string InviteUrl)> CreateInvitationAsync(
        string email, UserRole role, Guid createdByUserId, string baseUrl, CancellationToken ct = default)
    {
        var token = GenerateToken();
        var now = DateTime.UtcNow;

        var invitation = new UserInvitation
        {
            Id = Guid.NewGuid(),
            Email = email,
            Token = token,
            Role = role,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            ExpiresAt = now.AddHours(TokenExpiryHours)
        };

        _db.UserInvitations.Add(invitation);
        await _db.SaveChangesAsync(ct);

        var inviteUrl = $"{baseUrl}/invite/{token}";
        await _email.SendInvitationAsync(email, inviteUrl, ct);

        return (invitation, inviteUrl);
    }

    public Task<List<UserInvitation>> GetPendingInvitationsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return _db.UserInvitations
            .Where(i => i.UsedAt == null && i.ExpiresAt > now)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> RevokeInvitationAsync(Guid id, CancellationToken ct = default)
    {
        var invitation = await _db.UserInvitations.FindAsync([id], ct);
        if (invitation is null) return false;
        _db.UserInvitations.Remove(invitation);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<UserInvitation?> GetValidInvitationByTokenAsync(string token, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return _db.UserInvitations
            .FirstOrDefaultAsync(i => i.Token == token && i.UsedAt == null && i.ExpiresAt > now, ct);
    }

    public async Task<UserServiceResult> AcceptInvitationAsync(
        string token, string username, string password, CancellationToken ct = default)
    {
        if (password.Length < MinPasswordLength)
            return UserServiceResult.Fail($"Password must be at least {MinPasswordLength} characters.");

        var invitation = await GetValidInvitationByTokenAsync(token, ct);
        if (invitation is null)
            return UserServiceResult.Fail("Invitation is invalid or has expired.");

        var existing = await _users.GetByUsernameAsync(username, ct);
        if (existing is not null)
            return UserServiceResult.Fail("Username is already taken.");

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = username,
            AuthType = AuthType.Local,
            Role = invitation.Role,
            IsBuiltinAdmin = false,
            State = AccountState.Active,
            PasswordHash = _localAuth.HashPassword(password),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _users.CreateAsync(user, ct);

        invitation.UsedAt = now;
        invitation.UsedByUserId = user.Id;
        await _db.SaveChangesAsync(ct);

        return UserServiceResult.Ok();
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
