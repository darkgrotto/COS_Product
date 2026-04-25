using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public class MetricsService : IMetricsService
{
    private readonly AppDbContext _db;

    public MetricsService(AppDbContext db)
    {
        _db = db;
    }

    public Task<MetricsResult> GetMetricsAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default) =>
        BuildMetricsAsync(userId, filter, ct);

    public Task<MetricsResult> GetAggregateMetricsAsync(CollectionFilter filter, CancellationToken ct = default) =>
        BuildMetricsAsync(userId: null, filter, ct);

    // SUM/COUNT are computed at the database. Each content type runs as a single
    // GroupBy(_=>1).Select aggregation; FirstOrDefault returns null when no rows
    // match (empty user, no joinable card), in which case all aggregates default to 0.
    private async Task<MetricsResult> BuildMetricsAsync(Guid? userId, CollectionFilter filter, CancellationToken ct)
    {
        var result = new MetricsResult();

        // Cards
        var collectionQuery = _db.CollectionEntries
            .Join(_db.Cards, ce => ce.CardIdentifier, c => c.Identifier, (ce, c) => new { ce, c });
        if (userId.HasValue)
            collectionQuery = collectionQuery.Where(x => x.ce.UserId == userId.Value);

        if (!string.IsNullOrEmpty(filter.SetCode))
            collectionQuery = collectionQuery.Where(x => x.c.SetCode == filter.SetCode.ToLowerInvariant());
        if (!string.IsNullOrEmpty(filter.Color))
        {
            if (filter.Color == "C")
                collectionQuery = collectionQuery.Where(x => string.IsNullOrEmpty(x.c.Color));
            else
                collectionQuery = collectionQuery.Where(x => x.c.Color != null && x.c.Color.Contains(filter.Color));
        }
        if (!string.IsNullOrEmpty(filter.CardType))
            collectionQuery = collectionQuery.Where(x => x.c.CardType != null && x.c.CardType.Contains(filter.CardType));
        if (!string.IsNullOrEmpty(filter.Treatment))
            collectionQuery = collectionQuery.Where(x => x.ce.TreatmentKey == filter.Treatment);
        if (!string.IsNullOrEmpty(filter.Condition) && Enum.TryParse<CardCondition>(filter.Condition, true, out var condFilter))
            collectionQuery = collectionQuery.Where(x => x.ce.Condition == condFilter);
        if (filter.Autographed.HasValue)
            collectionQuery = collectionQuery.Where(x => x.ce.Autographed == filter.Autographed.Value);

        var collectionAgg = await collectionQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Value = g.Sum(x => (x.c.CurrentMarketValue ?? 0m) * x.ce.Quantity),
                ProfitLoss = g.Sum(x => ((x.c.CurrentMarketValue ?? 0m) - x.ce.AcquisitionPrice) * x.ce.Quantity),
                Count = g.Sum(x => x.ce.Quantity)
            })
            .FirstOrDefaultAsync(ct);

        decimal collectionValue = collectionAgg?.Value ?? 0m;
        decimal collectionPl = collectionAgg?.ProfitLoss ?? 0m;
        int collectionCount = collectionAgg?.Count ?? 0;

        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "cards",
            TotalValue = collectionValue,
            TotalProfitLoss = collectionPl,
            Count = collectionCount
        });

        // Serialized
        var serializedQuery = _db.SerializedEntries
            .Join(_db.Cards, se => se.CardIdentifier, c => c.Identifier, (se, c) => new { se, c });
        if (userId.HasValue)
            serializedQuery = serializedQuery.Where(x => x.se.UserId == userId.Value);

        var serializedAgg = await serializedQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Value = g.Sum(x => x.c.CurrentMarketValue ?? 0m),
                ProfitLoss = g.Sum(x => (x.c.CurrentMarketValue ?? 0m) - x.se.AcquisitionPrice),
                Count = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        decimal serializedValue = serializedAgg?.Value ?? 0m;
        decimal serializedPl = serializedAgg?.ProfitLoss ?? 0m;
        int serializedCount = serializedAgg?.Count ?? 0;

        result.SerializedCount = serializedCount;
        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "serialized",
            TotalValue = serializedValue,
            TotalProfitLoss = serializedPl,
            Count = serializedCount
        });

        // Slabs
        var slabQuery = _db.SlabEntries
            .Join(_db.Cards, se => se.CardIdentifier, c => c.Identifier, (se, c) => new { se, c });
        if (userId.HasValue)
            slabQuery = slabQuery.Where(x => x.se.UserId == userId.Value);

        var slabAgg = await slabQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Value = g.Sum(x => x.c.CurrentMarketValue ?? 0m),
                ProfitLoss = g.Sum(x => (x.c.CurrentMarketValue ?? 0m) - x.se.AcquisitionPrice),
                Count = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        decimal slabValue = slabAgg?.Value ?? 0m;
        decimal slabPl = slabAgg?.ProfitLoss ?? 0m;
        int slabCount = slabAgg?.Count ?? 0;

        result.SlabCount = slabCount;
        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "slabs",
            TotalValue = slabValue,
            TotalProfitLoss = slabPl,
            Count = slabCount
        });

        // Sealed
        var sealedQuery = _db.SealedInventoryEntries
            .Join(_db.SealedProducts, si => si.ProductIdentifier, sp => sp.Identifier, (si, sp) => new { si, sp });
        if (userId.HasValue)
            sealedQuery = sealedQuery.Where(x => x.si.UserId == userId.Value);

        var sealedAgg = await sealedQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Value = g.Sum(x => (x.sp.CurrentMarketValue ?? 0m) * x.si.Quantity),
                ProfitLoss = g.Sum(x => ((x.sp.CurrentMarketValue ?? 0m) - x.si.AcquisitionPrice) * x.si.Quantity),
                Count = g.Sum(x => x.si.Quantity)
            })
            .FirstOrDefaultAsync(ct);

        decimal sealedValue = sealedAgg?.Value ?? 0m;
        decimal sealedPl = sealedAgg?.ProfitLoss ?? 0m;
        int sealedCount = sealedAgg?.Count ?? 0;

        result.SealedProductCount = sealedCount;
        result.SealedProductValue = sealedValue;
        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "sealed",
            TotalValue = sealedValue,
            TotalProfitLoss = sealedPl,
            Count = sealedCount
        });

        result.TotalCardCount = collectionCount;
        result.TotalValue = collectionValue + serializedValue + slabValue + sealedValue;
        result.TotalProfitLoss = collectionPl + serializedPl + slabPl + sealedPl;

        return result;
    }


    public async Task<SetCompletionResult> GetSetCompletionAsync(Guid userId, string setCode, bool regularOnly, CancellationToken ct = default)
    {
        var setCodeLower = setCode.ToLowerInvariant();
        var set = await _db.Sets.FirstOrDefaultAsync(s => s.Code == setCodeLower, ct);
        if (set == null)
            return new SetCompletionResult { SetCode = setCodeLower.ToUpperInvariant() };

        var query = _db.CollectionEntries
            .Where(e => e.UserId == userId && e.CardIdentifier.StartsWith(setCodeLower));

        if (regularOnly)
            query = query.Where(e => e.TreatmentKey == "regular");

        var ownedIdentifiers = await query
            .Select(e => e.CardIdentifier)
            .Distinct()
            .CountAsync(ct);

        decimal percentage = set.TotalCards > 0
            ? Math.Round((decimal)ownedIdentifiers / set.TotalCards * 100, 1)
            : 0;

        return new SetCompletionResult
        {
            SetCode = set.Code.ToUpperInvariant(),
            SetName = set.Name,
            OwnedCount = ownedIdentifiers,
            TotalCards = set.TotalCards,
            Percentage = percentage
        };
    }

    public async Task<List<SetCompletionResult>> GetAllSetCompletionAsync(
        Guid userId, bool regularOnly, CollectionFilter? filter = null, CancellationToken ct = default)
    {
        var sets = await _db.Sets.ToListAsync(ct);

        // Owned count query - joins to cards so card-level filters can be applied
        var ownedQuery = _db.CollectionEntries
            .Join(_db.Cards, ce => ce.CardIdentifier, c => c.Identifier, (ce, c) => new { ce, c })
            .Where(x => x.ce.UserId == userId);

        if (regularOnly)
            ownedQuery = ownedQuery.Where(x => x.ce.TreatmentKey == "regular");

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.SetCode))
                ownedQuery = ownedQuery.Where(x => x.c.SetCode == filter.SetCode.ToLowerInvariant());
            if (!string.IsNullOrEmpty(filter.Color))
            {
                if (filter.Color == "C")
                    ownedQuery = ownedQuery.Where(x => string.IsNullOrEmpty(x.c.Color));
                else
                    ownedQuery = ownedQuery.Where(x => x.c.Color != null && x.c.Color.Contains(filter.Color));
            }
            if (!string.IsNullOrEmpty(filter.CardType))
                ownedQuery = ownedQuery.Where(x => x.c.CardType != null && x.c.CardType.Contains(filter.CardType));
            if (!string.IsNullOrEmpty(filter.Treatment))
                ownedQuery = ownedQuery.Where(x => x.ce.TreatmentKey == filter.Treatment);
            if (!string.IsNullOrEmpty(filter.Condition) && Enum.TryParse<CardCondition>(filter.Condition, true, out var condOwned))
                ownedQuery = ownedQuery.Where(x => x.ce.Condition == condOwned);
            if (filter.Autographed.HasValue)
                ownedQuery = ownedQuery.Where(x => x.ce.Autographed == filter.Autographed.Value);
        }

        var ownedBySet = await ownedQuery
            .GroupBy(x => x.c.SetCode)
            .Select(g => new
            {
                SetCode = g.Key,
                OwnedCount = g.Select(x => x.ce.CardIdentifier).Distinct().Count()
            })
            .ToDictionaryAsync(x => x.SetCode, x => x.OwnedCount, ct);

        // Value query - same filters as owned
        var valueQuery = _db.CollectionEntries
            .Join(_db.Cards, ce => ce.CardIdentifier, c => c.Identifier, (ce, c) => new { ce, c })
            .Where(x => x.ce.UserId == userId);

        if (regularOnly)
            valueQuery = valueQuery.Where(x => x.ce.TreatmentKey == "regular");

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.SetCode))
                valueQuery = valueQuery.Where(x => x.c.SetCode == filter.SetCode.ToLowerInvariant());
            if (!string.IsNullOrEmpty(filter.Color))
            {
                if (filter.Color == "C")
                    valueQuery = valueQuery.Where(x => string.IsNullOrEmpty(x.c.Color));
                else
                    valueQuery = valueQuery.Where(x => x.c.Color != null && x.c.Color.Contains(filter.Color));
            }
            if (!string.IsNullOrEmpty(filter.CardType))
                valueQuery = valueQuery.Where(x => x.c.CardType != null && x.c.CardType.Contains(filter.CardType));
            if (!string.IsNullOrEmpty(filter.Treatment))
                valueQuery = valueQuery.Where(x => x.ce.TreatmentKey == filter.Treatment);
            if (!string.IsNullOrEmpty(filter.Condition) && Enum.TryParse<CardCondition>(filter.Condition, true, out var condVal))
                valueQuery = valueQuery.Where(x => x.ce.Condition == condVal);
            if (filter.Autographed.HasValue)
                valueQuery = valueQuery.Where(x => x.ce.Autographed == filter.Autographed.Value);
        }

        var valueBySet = await valueQuery
            .GroupBy(x => x.c.SetCode)
            .Select(g => new
            {
                SetCode = g.Key,
                TotalValue = g.Sum(x => (x.c.CurrentMarketValue ?? 0) * x.ce.Quantity),
                TotalProfitLoss = g.Sum(x => ((x.c.CurrentMarketValue ?? 0) - x.ce.AcquisitionPrice) * x.ce.Quantity)
            })
            .ToDictionaryAsync(v => v.SetCode, ct);

        // Total cards per set - adjusted when a card-level filter (color/cardType) is active
        bool hasCardFilter = filter != null &&
            (!string.IsNullOrEmpty(filter.Color) || !string.IsNullOrEmpty(filter.CardType));

        Dictionary<string, int> totalBySet;
        if (hasCardFilter)
        {
            var cardQuery = _db.Cards.AsQueryable();
            if (!string.IsNullOrEmpty(filter!.Color))
            {
                if (filter.Color == "C")
                    cardQuery = cardQuery.Where(c => string.IsNullOrEmpty(c.Color));
                else
                    cardQuery = cardQuery.Where(c => c.Color != null && c.Color.Contains(filter.Color));
            }
            if (!string.IsNullOrEmpty(filter.CardType))
                cardQuery = cardQuery.Where(c => c.CardType != null && c.CardType.Contains(filter.CardType));

            totalBySet = await cardQuery
                .GroupBy(c => c.SetCode)
                .Select(g => new { SetCode = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.SetCode, x => x.Total, ct);
        }
        else
        {
            totalBySet = sets.ToDictionary(s => s.Code, s => s.TotalCards);
        }

        var results = new List<SetCompletionResult>(sets.Count);
        foreach (var set in sets)
        {
            ownedBySet.TryGetValue(set.Code, out var ownedCount);
            valueBySet.TryGetValue(set.Code, out var val);
            totalBySet.TryGetValue(set.Code, out var totalCards);

            if (hasCardFilter && totalCards == 0 && ownedCount == 0) continue;

            decimal percentage = totalCards > 0
                ? Math.Round((decimal)ownedCount / totalCards * 100, 1)
                : 0;

            results.Add(new SetCompletionResult
            {
                SetCode = set.Code.ToUpperInvariant(),
                SetName = set.Name,
                OwnedCount = ownedCount,
                TotalCards = totalCards,
                Percentage = percentage,
                TotalValue = val?.TotalValue,
                TotalProfitLoss = val?.TotalProfitLoss,
                ReleaseDate = set.ReleaseDate
            });
        }

        return results.OrderByDescending(r => r.Percentage).ToList();
    }

    public async Task<(List<TopCardResult> Results, int TotalCount)> GetTopCardsAsync(
        Guid userId, string metric, int limit, int offset, CollectionFilter filter, CancellationToken ct = default)
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
        if (!string.IsNullOrEmpty(filter.Treatment))
            query = query.Where(x => x.ce.TreatmentKey == filter.Treatment);
        if (!string.IsNullOrEmpty(filter.Condition) && Enum.TryParse<CardCondition>(filter.Condition, true, out var cond))
            query = query.Where(x => x.ce.Condition == cond);
        if (filter.Autographed.HasValue)
            query = query.Where(x => x.ce.Autographed == filter.Autographed.Value);

        var grouped = query
            .GroupBy(x => new { x.c.Identifier, x.c.Name, SetCode = x.c.SetCode, x.c.CurrentMarketValue })
            .Select(g => new
            {
                CardIdentifier = g.Key.Identifier,
                CardName = g.Key.Name,
                SetCode = g.Key.SetCode,
                MarketValue = g.Key.CurrentMarketValue,
                TotalQuantity = g.Sum(x => x.ce.Quantity),
                TotalValue = g.Sum(x => (x.c.CurrentMarketValue ?? 0m) * x.ce.Quantity)
            });

        var totalCount = await grouped.CountAsync(ct);

        var ordered = metric == "frequency"
            ? grouped.OrderByDescending(x => x.TotalQuantity).ThenBy(x => x.CardIdentifier)
            : grouped.OrderByDescending(x => x.TotalValue).ThenBy(x => x.CardIdentifier);

        var rows = await ordered.Skip(offset).Take(limit).ToListAsync(ct);

        var results = rows.Select(x => new TopCardResult
        {
            CardIdentifier = x.CardIdentifier.ToUpperInvariant(),
            CardName = x.CardName,
            SetCode = x.SetCode.ToUpperInvariant(),
            TotalQuantity = x.TotalQuantity,
            TotalValue = x.TotalValue,
            MarketValue = x.MarketValue
        }).ToList();

        return (results, totalCount);
    }
}
