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
        Guid userId, string? categorySlug, string? subTypeSlug, CancellationToken ct = default) =>
        BuildFilteredQuery(userId, categorySlug, subTypeSlug).ToListAsync(ct);

    public async Task<(List<SealedInventoryEntry> Items, int Total)> GetByUserPagedAsync(
        Guid userId, string? categorySlug, string? subTypeSlug, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(userId, categorySlug, subTypeSlug);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    private IQueryable<SealedInventoryEntry> BuildFilteredQuery(Guid userId, string? categorySlug, string? subTypeSlug)
    {
        var query = _db.SealedInventoryEntries.Where(e => e.UserId == userId);
        if (!string.IsNullOrEmpty(categorySlug))
            query = query.Where(e => e.CategorySlug == categorySlug);
        if (!string.IsNullOrEmpty(subTypeSlug))
            query = query.Where(e => e.SubTypeSlug == subTypeSlug);
        return query;
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

    public async Task<int> BulkDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var entries = await _db.SealedInventoryEntries
            .Where(e => idList.Contains(e.Id) && e.UserId == userId)
            .ToListAsync(ct);
        _db.SealedInventoryEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
        return entries.Count;
    }

    public async Task<int> BulkSetConditionAsync(IEnumerable<Guid> ids, Guid userId, string condition, CancellationToken ct = default)
    {
        if (!Enum.TryParse<CountOrSell.Domain.Models.CardCondition>(condition, true, out var parsed))
            return 0;
        var idList = ids.ToList();
        var entries = await _db.SealedInventoryEntries
            .Where(e => idList.Contains(e.Id) && e.UserId == userId)
            .ToListAsync(ct);
        foreach (var e in entries) e.Condition = parsed;
        await _db.SaveChangesAsync(ct);
        return entries.Count;
    }

    public async Task<int> BulkSetAcquisitionDateAsync(IEnumerable<Guid> ids, Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var entries = await _db.SealedInventoryEntries
            .Where(e => idList.Contains(e.Id) && e.UserId == userId)
            .ToListAsync(ct);
        foreach (var e in entries) e.AcquisitionDate = date;
        await _db.SaveChangesAsync(ct);
        return entries.Count;
    }

    public async Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _db.SealedInventoryEntries.Where(e => e.UserId == userId).ToListAsync(ct);
        _db.SealedInventoryEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
    }
}
