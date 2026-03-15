using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class CollectionRepository : ICollectionRepository
{
    private readonly AppDbContext _db;
    public CollectionRepository(AppDbContext db) => _db = db;

    public Task<CollectionEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.CollectionEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<CollectionEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.CollectionEntries.Where(e => e.UserId == userId).ToListAsync(ct);

    public async Task<CollectionEntry> CreateAsync(CollectionEntry entry, CancellationToken ct = default)
    {
        _db.CollectionEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<CollectionEntry> UpdateAsync(CollectionEntry entry, CancellationToken ct = default)
    {
        _db.CollectionEntries.Update(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _db.CollectionEntries.FindAsync(new object[] { id }, ct);
        if (entry != null)
        {
            _db.CollectionEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
        }
    }
}
