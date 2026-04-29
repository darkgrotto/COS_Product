using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
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

    public Task<List<SlabEntry>> GetByUserFilteredAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default) =>
        BuildFilteredQuery(userId, filter).ToListAsync(ct);

    public async Task<(List<SlabEntry> Items, int Total)> GetByUserPagedAsync(
        Guid userId, CollectionFilter? filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = filter != null && HasFilters(filter)
            ? BuildFilteredQuery(userId, filter)
            : _db.SlabEntries.Where(e => e.UserId == userId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    private static bool HasFilters(CollectionFilter filter) =>
        !string.IsNullOrEmpty(filter.SetCode) || !string.IsNullOrEmpty(filter.Treatment) ||
        !string.IsNullOrEmpty(filter.Condition) || filter.Autographed.HasValue ||
        !string.IsNullOrEmpty(filter.GradingAgency);

    private IQueryable<SlabEntry> BuildFilteredQuery(Guid userId, CollectionFilter filter)
    {
        var query = _db.SlabEntries
            .Join(_db.Cards, se => se.CardIdentifier, c => c.Identifier, (se, c) => new { se, c })
            .Where(x => x.se.UserId == userId);

        if (!string.IsNullOrEmpty(filter.SetCode))
            query = query.Where(x => x.c.SetCode == filter.SetCode.ToLowerInvariant());
        if (!string.IsNullOrEmpty(filter.Treatment))
            query = query.Where(x => x.se.TreatmentKey == filter.Treatment);
        if (!string.IsNullOrEmpty(filter.Condition) &&
            Enum.TryParse<CardCondition>(filter.Condition, true, out var cond))
            query = query.Where(x => x.se.Condition == cond);
        if (filter.Autographed.HasValue)
            query = query.Where(x => x.se.Autographed == filter.Autographed.Value);
        if (!string.IsNullOrEmpty(filter.GradingAgency))
            query = query.Where(x => x.se.GradingAgencyCode == filter.GradingAgency.ToLowerInvariant());

        return query.Select(x => x.se);
    }

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

    public async Task<int> BulkDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var entries = await _db.SlabEntries
            .Where(e => idList.Contains(e.Id) && e.UserId == userId)
            .ToListAsync(ct);
        _db.SlabEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
        return entries.Count;
    }

    public async Task<int> BulkSetConditionAsync(IEnumerable<Guid> ids, Guid userId, string condition, CancellationToken ct = default)
    {
        if (!Enum.TryParse<CountOrSell.Domain.Models.CardCondition>(condition, true, out var parsed))
            return 0;
        var idList = ids.ToList();
        var entries = await _db.SlabEntries
            .Where(e => idList.Contains(e.Id) && e.UserId == userId)
            .ToListAsync(ct);
        foreach (var e in entries) e.Condition = parsed;
        await _db.SaveChangesAsync(ct);
        return entries.Count;
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
