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

    public async Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _db.SlabEntries.Where(e => e.UserId == userId).ToListAsync(ct);
        _db.SlabEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountByAgencyCodeAsync(string agencyCode, CancellationToken ct = default) =>
        _db.SlabEntries.CountAsync(e => e.GradingAgencyCode == agencyCode, ct);

    public async Task RemapAgencyCodeAsync(string oldCode, string newCode, CancellationToken ct = default)
    {
        var entries = await _db.SlabEntries.Where(e => e.GradingAgencyCode == oldCode).ToListAsync(ct);
        foreach (var entry in entries)
            entry.GradingAgencyCode = newCode;
        await _db.SaveChangesAsync(ct);
    }
}
