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

    public Task<List<CollectionEntry>> GetByUserFilteredAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default) =>
        BuildFilteredQuery(userId, filter).ToListAsync(ct);

    public async Task<(List<CollectionEntry> Items, int Total)> GetByUserPagedAsync(
        Guid userId, CollectionFilter? filter, string? sort, string? sortDir,
        int page, int pageSize, CancellationToken ct = default)
    {
        // LEFT JOIN cards so orphan entries (card removed by an update) still appear when sorting/filtering.
        var joined =
            from e in _db.CollectionEntries.Where(x => x.UserId == userId)
            join c in _db.Cards on e.CardIdentifier equals c.Identifier into cards
            from c in cards.DefaultIfEmpty()
            select new { e, c };

        if (filter != null && HasFilters(filter))
        {
            if (!string.IsNullOrEmpty(filter.SetCode))
                joined = joined.Where(x => x.c != null && x.c.SetCode == filter.SetCode.ToLowerInvariant());

            if (!string.IsNullOrEmpty(filter.Color))
            {
                if (filter.Color == "C")
                    joined = joined.Where(x => x.c != null && string.IsNullOrEmpty(x.c.Color));
                else
                    joined = joined.Where(x => x.c != null && x.c.Color != null && x.c.Color.Contains(filter.Color));
            }

            if (!string.IsNullOrEmpty(filter.CardType))
                joined = joined.Where(x => x.c != null && x.c.CardType != null && x.c.CardType.Contains(filter.CardType));

            if (!string.IsNullOrEmpty(filter.CardSubtype))
                joined = joined.Where(x => x.c != null && x.c.CardSubtypes != null && x.c.CardSubtypes.Contains(filter.CardSubtype));

            if (!string.IsNullOrEmpty(filter.Treatment))
                joined = joined.Where(x => x.e.TreatmentKey == filter.Treatment);

            if (!string.IsNullOrEmpty(filter.Condition) &&
                Enum.TryParse<CardCondition>(filter.Condition, true, out var cond))
                joined = joined.Where(x => x.e.Condition == cond);

            if (filter.Autographed.HasValue)
                joined = joined.Where(x => x.e.Autographed == filter.Autographed.Value);

            if (filter.IsReserved == true)
                joined = joined.Where(x => x.c != null && x.c.IsReserved);

            if (filter.HasPhyrexianMana == true)
                joined = joined.Where(x => x.c != null && x.c.ManaCost != null && x.c.ManaCost.Contains("/P}"));

            if (filter.HasHybridMana == true)
                joined = joined.Where(x => x.c != null && x.c.ManaCost != null &&
                    (x.c.ManaCost.Contains("/W}") || x.c.ManaCost.Contains("/U}") ||
                     x.c.ManaCost.Contains("/B}") || x.c.ManaCost.Contains("/R}") ||
                     x.c.ManaCost.Contains("/G}")));
        }

        var total = await joined.CountAsync(ct);

        // LEFT JOIN per-treatment price + treatments so sort keys match what the user sees.
        // Effective MV = treatment price when row exists, else card MV.
        var withSortable =
            from x in joined
            join p in _db.CardPrices
                on new { x.e.CardIdentifier, x.e.TreatmentKey }
                equals new { p.CardIdentifier, p.TreatmentKey } into prices
            from p in prices.DefaultIfEmpty()
            join t in _db.Treatments on x.e.TreatmentKey equals t.Key into treatments
            from t in treatments.DefaultIfEmpty()
            select new
            {
                Entry = x.e,
                CardName = x.c != null ? x.c.Name : x.e.CardIdentifier,
                SetCode = x.c != null ? x.c.SetCode : string.Empty,
                MarketValue = p != null ? p.PriceUsd : (x.c != null ? x.c.CurrentMarketValue : (decimal?)null),
                TreatmentSort = t != null ? t.SortOrder : int.MaxValue,
                TreatmentLabel = t != null ? t.DisplayName : x.e.TreatmentKey
            };

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        var ordered = (sort?.ToLowerInvariant()) switch
        {
            "card" => desc
                ? withSortable.OrderByDescending(x => x.CardName)
                : withSortable.OrderBy(x => x.CardName),
            "identifier" => desc
                ? withSortable.OrderByDescending(x => x.Entry.CardIdentifier)
                : withSortable.OrderBy(x => x.Entry.CardIdentifier),
            "set" => desc
                ? withSortable.OrderByDescending(x => x.SetCode)
                : withSortable.OrderBy(x => x.SetCode),
            "treatment" => desc
                ? withSortable.OrderByDescending(x => x.TreatmentSort).ThenByDescending(x => x.TreatmentLabel)
                : withSortable.OrderBy(x => x.TreatmentSort).ThenBy(x => x.TreatmentLabel),
            "qty" => desc
                ? withSortable.OrderByDescending(x => x.Entry.Quantity)
                : withSortable.OrderBy(x => x.Entry.Quantity),
            "condition" => desc
                ? withSortable.OrderByDescending(x => x.Entry.Condition)
                : withSortable.OrderBy(x => x.Entry.Condition),
            "market" => desc
                ? withSortable.OrderByDescending(x => x.MarketValue ?? decimal.MinValue)
                : withSortable.OrderBy(x => x.MarketValue ?? decimal.MinValue),
            "acq" => desc
                ? withSortable.OrderByDescending(x => x.Entry.AcquisitionPrice)
                : withSortable.OrderBy(x => x.Entry.AcquisitionPrice),
            "pl" => desc
                ? withSortable.OrderByDescending(x => ((x.MarketValue ?? decimal.MinValue) - x.Entry.AcquisitionPrice) * x.Entry.Quantity)
                : withSortable.OrderBy(x => ((x.MarketValue ?? decimal.MinValue) - x.Entry.AcquisitionPrice) * x.Entry.Quantity),
            _ => withSortable.OrderByDescending(x => x.Entry.CreatedAt),
        };

        var items = await ordered
            .ThenByDescending(x => x.Entry.Id) // stable secondary key for deterministic pagination
            .Select(x => x.Entry)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    private static bool HasFilters(CollectionFilter filter) =>
        !string.IsNullOrEmpty(filter.SetCode) || !string.IsNullOrEmpty(filter.Color) ||
        !string.IsNullOrEmpty(filter.CardType) || !string.IsNullOrEmpty(filter.CardSubtype) ||
        !string.IsNullOrEmpty(filter.Treatment) ||
        !string.IsNullOrEmpty(filter.Condition) || filter.Autographed.HasValue ||
        filter.IsReserved.HasValue || filter.HasPhyrexianMana.HasValue || filter.HasHybridMana.HasValue;

    private IQueryable<CollectionEntry> BuildFilteredQuery(Guid userId, CollectionFilter filter)
    {
        var query = _db.CollectionEntries
            .Join(_db.Cards, ce => ce.CardIdentifier, c => c.Identifier, (ce, c) => new { ce, c })
            .Where(x => x.ce.UserId == userId);

        if (!string.IsNullOrEmpty(filter.SetCode))
            query = query.Where(x => x.c.SetCode == filter.SetCode.ToLowerInvariant());

        if (!string.IsNullOrEmpty(filter.Color))
        {
            if (filter.Color == "C")
                query = query.Where(x => string.IsNullOrEmpty(x.c.Color));
            else
                query = query.Where(x => x.c.Color != null && x.c.Color.Contains(filter.Color));
        }

        if (!string.IsNullOrEmpty(filter.CardType))
            query = query.Where(x => x.c.CardType != null && x.c.CardType.Contains(filter.CardType));

        if (!string.IsNullOrEmpty(filter.CardSubtype))
            query = query.Where(x => x.c.CardSubtypes != null && x.c.CardSubtypes.Contains(filter.CardSubtype));

        if (!string.IsNullOrEmpty(filter.Treatment))
            query = query.Where(x => x.ce.TreatmentKey == filter.Treatment);

        if (!string.IsNullOrEmpty(filter.Condition) &&
            Enum.TryParse<CardCondition>(filter.Condition, true, out var cond))
            query = query.Where(x => x.ce.Condition == cond);

        if (filter.Autographed.HasValue)
            query = query.Where(x => x.ce.Autographed == filter.Autographed.Value);

        if (filter.IsReserved == true)
            query = query.Where(x => x.c.IsReserved);

        if (filter.HasPhyrexianMana == true)
            query = query.Where(x => x.c.ManaCost != null && x.c.ManaCost.Contains("/P}"));

        if (filter.HasHybridMana == true)
            query = query.Where(x => x.c.ManaCost != null &&
                (x.c.ManaCost.Contains("/W}") || x.c.ManaCost.Contains("/U}") ||
                 x.c.ManaCost.Contains("/B}") || x.c.ManaCost.Contains("/R}") ||
                 x.c.ManaCost.Contains("/G}")));

        return query.Select(x => x.ce);
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
