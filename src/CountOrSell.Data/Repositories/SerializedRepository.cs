using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
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

    public Task<List<SerializedEntry>> GetByUserFilteredAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default) =>
        BuildFilteredQuery(userId, filter).ToListAsync(ct);

    public async Task<(List<SerializedEntry> Items, int Total)> GetByUserPagedAsync(
        Guid userId, CollectionFilter? filter, string? sort, string? sortDir,
        int page, int pageSize, CancellationToken ct = default)
    {
        // LEFT JOIN cards so sort/filter can reference card fields without dropping orphans.
        var joined =
            from e in _db.SerializedEntries.Where(x => x.UserId == userId)
            join c in _db.Cards on e.CardIdentifier equals c.Identifier into cards
            from c in cards.DefaultIfEmpty()
            select new { e, c };

        if (filter != null && HasFilters(filter))
        {
            if (!string.IsNullOrEmpty(filter.SetCode))
                joined = joined.Where(x => x.c != null && x.c.SetCode == filter.SetCode.ToLowerInvariant());
            if (!string.IsNullOrEmpty(filter.Treatment))
                joined = joined.Where(x => x.e.TreatmentKey == filter.Treatment);
            if (!string.IsNullOrEmpty(filter.Condition) &&
                Enum.TryParse<CardCondition>(filter.Condition, true, out var cond))
                joined = joined.Where(x => x.e.Condition == cond);
            if (filter.Autographed.HasValue)
                joined = joined.Where(x => x.e.Autographed == filter.Autographed.Value);
        }

        var total = await joined.CountAsync(ct);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        // Sort keys mirror what the UI exposes (Serialized.tsx: card, identifier).
        var ordered = (sort?.ToLowerInvariant()) switch
        {
            "card" => desc
                ? joined.OrderByDescending(x => x.c != null ? x.c.Name : x.e.CardIdentifier)
                : joined.OrderBy(x => x.c != null ? x.c.Name : x.e.CardIdentifier),
            "identifier" => desc
                ? joined.OrderByDescending(x => x.e.CardIdentifier)
                : joined.OrderBy(x => x.e.CardIdentifier),
            _ => joined.OrderByDescending(x => x.e.CreatedAt),
        };

        var items = await ordered
            .ThenByDescending(x => x.e.Id)
            .Select(x => x.e)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    private static bool HasFilters(CollectionFilter filter) =>
        !string.IsNullOrEmpty(filter.SetCode) || !string.IsNullOrEmpty(filter.Treatment) ||
        !string.IsNullOrEmpty(filter.Condition) || filter.Autographed.HasValue;

    private IQueryable<SerializedEntry> BuildFilteredQuery(Guid userId, CollectionFilter filter)
    {
        var query = _db.SerializedEntries
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

        return query.Select(x => x.se);
    }

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

    public async Task<int> BulkDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var entries = await _db.SerializedEntries
            .Where(e => idList.Contains(e.Id) && e.UserId == userId)
            .ToListAsync(ct);
        _db.SerializedEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
        return entries.Count;
    }

    public async Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _db.SerializedEntries.Where(e => e.UserId == userId).ToListAsync(ct);
        _db.SerializedEntries.RemoveRange(entries);
        await _db.SaveChangesAsync(ct);
    }
}
