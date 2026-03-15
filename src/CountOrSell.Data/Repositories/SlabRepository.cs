using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class SlabRepository : ISlabRepository
{
    private readonly AppDbContext _db;
    public SlabRepository(AppDbContext db) => _db = db;

    public Task<SlabEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SlabEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<SlabEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.SlabEntries.Where(e => e.UserId == userId).ToListAsync(ct);

    public async Task<SlabEntry> CreateAsync(SlabEntry entry, CancellationToken ct = default)
    {
        _db.SlabEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<SlabEntry> UpdateAsync(SlabEntry entry, CancellationToken ct = default)
    {
        _db.SlabEntries.Update(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _db.SlabEntries.FindAsync(new object[] { id }, ct);
        if (entry != null)
        {
            _db.SlabEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
        }
    }
}
