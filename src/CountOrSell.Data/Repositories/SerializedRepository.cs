using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class SerializedRepository : ISerializedRepository
{
    private readonly AppDbContext _db;
    public SerializedRepository(AppDbContext db) => _db = db;

    public Task<SerializedEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SerializedEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<SerializedEntry>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.SerializedEntries.Where(e => e.UserId == userId).ToListAsync(ct);

    public async Task<SerializedEntry> CreateAsync(SerializedEntry entry, CancellationToken ct = default)
    {
        _db.SerializedEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<SerializedEntry> UpdateAsync(SerializedEntry entry, CancellationToken ct = default)
    {
        _db.SerializedEntries.Update(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _db.SerializedEntries.FindAsync(new object[] { id }, ct);
        if (entry != null)
        {
            _db.SerializedEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _db.SerializedEntries.Where(e => e.UserId == userId).ToListAsync(ct);
        _db.SerializedEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
    }
}
