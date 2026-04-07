using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
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

    public Task<List<CollectionEntry>> GetByUserFilteredAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default)
    {
        var query = _db.CollectionEntries
            .Join(_db.Cards, ce => ce.CardIdentifier, c => c.Identifier, (ce, c) => new { ce, c })
            .Where(x => x.ce.UserId == userId);

        if (!string.IsNullOrEmpty(filter.SetCode))
            query = query.Where(x => x.c.SetCode == filter.SetCode.ToLowerInvariant());

        if (!string.IsNullOrEmpty(filter.Color))
            query = query.Where(x => x.c.Color != null && x.c.Color.Contains(filter.Color));

        if (!string.IsNullOrEmpty(filter.CardType))
            query = query.Where(x => x.c.CardType != null && x.c.CardType.Contains(filter.CardType));

        if (!string.IsNullOrEmpty(filter.Treatment))
            query = query.Where(x => x.ce.TreatmentKey == filter.Treatment);

        if (!string.IsNullOrEmpty(filter.Condition) &&
            Enum.TryParse<CardCondition>(filter.Condition, true, out var cond))
            query = query.Where(x => x.ce.Condition == cond);

        if (filter.Autographed.HasValue)
            query = query.Where(x => x.ce.Autographed == filter.Autographed.Value);

        return query.Select(x => x.ce).ToListAsync(ct);
    }

    public Task<List<ReservedCollectionEntry>> GetReservedEntriesForUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.CollectionEntries
            .Where(e => e.UserId == userId)
            .Join(_db.Cards.Where(c => c.IsReserved),
                e => e.CardIdentifier,
                c => c.Identifier,
                (e, c) => new ReservedCollectionEntry
                {
                    EntryId = e.Id,
                    CardIdentifier = e.CardIdentifier.ToUpper(),
                    CardName = c.Name,
                    SetCode = c.SetCode.ToUpper(),
                    CardType = c.CardType,
                    Treatment = e.TreatmentKey,
                    Quantity = e.Quantity,
                    Condition = e.Condition.ToString(),
                    Autographed = e.Autographed,
                    AcquisitionPrice = e.AcquisitionPrice,
                    MarketValue = c.CurrentMarketValue
                })
            .OrderBy(e => e.SetCode)
            .ThenBy(e => e.CardIdentifier)
            .ToListAsync(ct);

    public async Task<CollectionEntry> CreateAsync(CollectionEntry entry, CancellationToken ct = default)
    {
        _db.CollectionEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task BulkCreateAsync(List<CollectionEntry> entries, CancellationToken ct = default)
    {
        _db.CollectionEntries.AddRange(entries);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<HashSet<string>> GetOwnedIdentifiersBySetAsync(Guid userId, string setCode, CancellationToken ct = default)
    {
        var identifiers = await _db.CollectionEntries
            .Join(_db.Cards, ce => ce.CardIdentifier, c => c.Identifier, (ce, c) => new { ce, c })
            .Where(x => x.ce.UserId == userId && x.c.SetCode == setCode.ToLowerInvariant())
            .Select(x => x.ce.CardIdentifier)
            .Distinct()
            .ToListAsync(ct);
        return identifiers.ToHashSet();
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

    public async Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _db.CollectionEntries.Where(e => e.UserId == userId).ToListAsync(ct);
        _db.CollectionEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> BulkDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var entries = await _db.CollectionEntries
            .Where(e => idList.Contains(e.Id) && e.UserId == userId)
            .ToListAsync(ct);
        _db.CollectionEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
        return entries.Count;
    }

    public async Task<int> BulkSetTreatmentAsync(IEnumerable<Guid> ids, Guid userId, string treatment, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var entries = await _db.CollectionEntries
            .Where(e => idList.Contains(e.Id) && e.UserId == userId)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var e in entries)
        {
            e.TreatmentKey = treatment;
            e.UpdatedAt = now;
        }
        await _db.SaveChangesAsync(ct);
        return entries.Count;
    }

    public async Task<int> BulkSetAcquisitionDateAsync(IEnumerable<Guid> ids, Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var entries = await _db.CollectionEntries
            .Where(e => idList.Contains(e.Id) && e.UserId == userId)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var e in entries)
        {
            e.AcquisitionDate = date;
            e.UpdatedAt = now;
        }
        await _db.SaveChangesAsync(ct);
        return entries.Count;
    }
}
