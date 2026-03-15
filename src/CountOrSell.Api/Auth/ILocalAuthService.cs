using CountOrSell.Domain.Models;

namespace CountOrSell.Api.Auth;

public interface ILocalAuthService
{
    Task<User?> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
