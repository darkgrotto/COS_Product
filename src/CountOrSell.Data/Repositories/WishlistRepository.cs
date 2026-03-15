using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly AppDbContext _db;
    public WishlistRepository(AppDbContext db) => _db = db;

    public Task<List<WishlistEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.WishlistEntries.Where(e => e.UserId == userId).ToListAsync(ct);

    public async Task<WishlistEntry> CreateAsync(WishlistEntry entry, CancellationToken ct = default)
    {
        _db.WishlistEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _db.WishlistEntries.FindAsync(new object[] { id }, ct);
        if (entry != null)
        {
            _db.WishlistEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
        }
    }
}
