using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByOAuthAsync(string provider, string providerUserId, CancellationToken ct = default);
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task<User> UpdateAsync(User user, CancellationToken ct = default);
    Task<int> CountLocalAdminsAsync(CancellationToken ct = default);
    Task<int> CountLocalAdminsExcludingAsync(Guid excludeUserId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
