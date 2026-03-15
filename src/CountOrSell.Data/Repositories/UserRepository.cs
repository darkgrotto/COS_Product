using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Preferences).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<User?> GetByOAuthAsync(string provider, string providerUserId, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.OAuthProvider == provider && u.OAuthProviderUserId == providerUserId, ct);

    public Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
        _db.Users.Include(u => u.Preferences).ToListAsync(ct);

    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public Task<int> CountLocalAdminsAsync(CancellationToken ct = default) =>
        _db.Users.CountAsync(u =>
            u.AuthType == AuthType.Local &&
            u.Role == UserRole.Admin &&
            u.State == AccountState.Active, ct);

    public Task<int> CountLocalAdminsExcludingAsync(Guid excludeUserId, CancellationToken ct = default) =>
        _db.Users.CountAsync(u =>
            u.Id != excludeUserId &&
            u.AuthType == AuthType.Local &&
            u.Role == UserRole.Admin &&
            u.State == AccountState.Active, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.Id == id, ct);
}
