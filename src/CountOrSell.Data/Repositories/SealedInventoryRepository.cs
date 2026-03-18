using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class SealedInventoryRepository : ISealedInventoryRepository
{
    private readonly AppDbContext _db;
    public SealedInventoryRepository(AppDbContext db) => _db = db;

    public Task<SealedInventoryEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SealedInventoryEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<SealedInventoryEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.SealedInventoryEntries.Where(e => e.UserId == userId).ToListAsync(ct);

    public Task<List<SealedInventoryEntry>> GetByUserFilteredAsync(
        Guid userId, string? categorySlug, string? subTypeSlug, CancellationToken ct = default)
    {
        var query = _db.SealedInventoryEntries
            .Join(_db.SealedProducts, i => i.ProductIdentifier, p => p.Identifier, (i, p) => new { i, p })
            .Where(x => x.i.UserId == userId);

        if (!string.IsNullOrEmpty(categorySlug))
            query = query.Where(x => x.p.CategorySlug == categorySlug);

        if (!string.IsNullOrEmpty(subTypeSlug))
            query = query.Where(x => x.p.SubTypeSlug == subTypeSlug);

        return query.Select(x => x.i).ToListAsync(ct);
    }

    public async Task<SealedInventoryEntry> CreateAsync(SealedInventoryEntry entry, CancellationToken ct = default)
    {
        _db.SealedInventoryEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<SealedInventoryEntry> UpdateAsync(SealedInventoryEntry entry, CancellationToken ct = default)
    {
        _db.SealedInventoryEntries.Update(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _db.SealedInventoryEntries.FindAsync(new object[] { id }, ct);
        if (entry != null)
        {
            _db.SealedInventoryEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _db.SealedInventoryEntries.Where(e => e.UserId == userId).ToListAsync(ct);
        _db.SealedInventoryEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
    }
}
