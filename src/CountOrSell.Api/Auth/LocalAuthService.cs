using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Api.Auth;

public class LocalAuthService : ILocalAuthService
{
    private readonly IUserRepository _users;

    public LocalAuthService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<User?> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user is null) return null;
        if (user.AuthType != AuthType.Local) return null;
        if (user.State != AccountState.Active) return null;
        if (user.PasswordHash is null) return null;
        if (!VerifyPassword(password, user.PasswordHash)) return null;
        return user;
    }

    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool VerifyPassword(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
