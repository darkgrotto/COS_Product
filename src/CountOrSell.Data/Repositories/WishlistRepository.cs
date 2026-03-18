using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly AppDbContext _db;
    public WishlistRepository(AppDbContext db) => _db = db;

    public Task<List<WishlistEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.WishlistEntries.Where(e => e.UserId == userId).ToListAsync(ct);

    public async Task<List<(WishlistEntry Entry, Card? Card)>> GetByUserWithCardsAsync(
        Guid userId, CollectionFilter filter, CancellationToken ct = default)
    {
        var query = _db.WishlistEntries
            .Where(e => e.UserId == userId)
            .GroupJoin(_db.Cards, e => e.CardIdentifier, c => c.Identifier, (e, cs) => new { e, cs })
            .SelectMany(x => x.cs.DefaultIfEmpty(), (x, c) => new { x.e, c });

        if (!string.IsNullOrEmpty(filter.SetCode))
            query = query.Where(x => x.c != null && x.c.SetCode == filter.SetCode.ToLowerInvariant());
        if (!string.IsNullOrEmpty(filter.Color))
            query = query.Where(x => x.c != null && x.c.Color == filter.Color);
        if (!string.IsNullOrEmpty(filter.CardType))
            query = query.Where(x => x.c != null && x.c.CardType != null && x.c.CardType.Contains(filter.CardType));

        var rows = await query.ToListAsync(ct);
        return rows.Select(x => (x.e, x.c)).ToList();
    }

    public async Task<WishlistEntry> CreateAsync(WishlistEntry entry, CancellationToken ct = default)
    {
        _db.WishlistEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public Task<WishlistEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.WishlistEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _db.WishlistEntries.FindAsync(new object[] { id }, ct);
        if (entry != null)
        {
            _db.WishlistEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _db.WishlistEntries.Where(e => e.UserId == userId).ToListAsync(ct);
        _db.WishlistEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
    }
}
