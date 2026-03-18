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

    public async Task<MetricsResult> GetMetricsAsync(Guid userId, CollectionFilter filter, CancellationToken ct = default)
    {
        var result = new MetricsResult();

        // Collection entries value
        var collectionQuery = _db.CollectionEntries
            .Join(_db.Cards, ce => ce.CardIdentifier, c => c.Identifier, (ce, c) => new { ce, c })
            .Where(x => x.ce.UserId == userId);

        if (!string.IsNullOrEmpty(filter.SetCode))
            collectionQuery = collectionQuery.Where(x => x.c.SetCode == filter.SetCode.ToLowerInvariant());
        if (!string.IsNullOrEmpty(filter.Color))
            collectionQuery = collectionQuery.Where(x => x.c.Color == filter.Color);
        if (!string.IsNullOrEmpty(filter.CardType))
            collectionQuery = collectionQuery.Where(x => x.c.CardType != null && x.c.CardType.Contains(filter.CardType));
        if (!string.IsNullOrEmpty(filter.Treatment))
            collectionQuery = collectionQuery.Where(x => x.ce.TreatmentKey == filter.Treatment);
        if (!string.IsNullOrEmpty(filter.Condition) && Enum.TryParse<CardCondition>(filter.Condition, true, out var condFilter))
            collectionQuery = collectionQuery.Where(x => x.ce.Condition == condFilter);
        if (filter.Autographed.HasValue)
            collectionQuery = collectionQuery.Where(x => x.ce.Autographed == filter.Autographed.Value);

        var collectionEntries = await collectionQuery
            .Select(x => new
            {
                x.ce.Quantity,
                x.ce.AcquisitionPrice,
                MarketValue = x.c.CurrentMarketValue
            })
            .ToListAsync(ct);

        decimal collectionValue = collectionEntries.Sum(e => (e.MarketValue ?? 0) * e.Quantity);
        decimal collectionPl = collectionEntries.Sum(e => ((e.MarketValue ?? 0) - e.AcquisitionPrice) * e.Quantity);
        int collectionCount = collectionEntries.Sum(e => e.Quantity);

        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "cards",
            TotalValue = collectionValue,
            TotalProfitLoss = collectionPl,
            Count = collectionCount
        });

        // Serialized entries
        var serializedEntries = await _db.SerializedEntries
            .Join(_db.Cards, se => se.CardIdentifier, c => c.Identifier, (se, c) => new { se, c })
            .Where(x => x.se.UserId == userId)
            .Select(x => new
            {
                x.se.AcquisitionPrice,
                MarketValue = x.c.CurrentMarketValue
            })
            .ToListAsync(ct);

        decimal serializedValue = serializedEntries.Sum(e => e.MarketValue ?? 0);
        decimal serializedPl = serializedEntries.Sum(e => (e.MarketValue ?? 0) - e.AcquisitionPrice);

        result.SerializedCount = serializedEntries.Count;
        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "serialized",
            TotalValue = serializedValue,
            TotalProfitLoss = serializedPl,
            Count = serializedEntries.Count
        });

        // Slab entries
        var slabEntries = await _db.SlabEntries
            .Join(_db.Cards, se => se.CardIdentifier, c => c.Identifier, (se, c) => new { se, c })
            .Where(x => x.se.UserId == userId)
            .Select(x => new
            {
                x.se.AcquisitionPrice,
                MarketValue = x.c.CurrentMarketValue
            })
            .ToListAsync(ct);

        decimal slabValue = slabEntries.Sum(e => e.MarketValue ?? 0);
        decimal slabPl = slabEntries.Sum(e => (e.MarketValue ?? 0) - e.AcquisitionPrice);

        result.SlabCount = slabEntries.Count;
        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "slabs",
            TotalValue = slabValue,
            TotalProfitLoss = slabPl,
            Count = slabEntries.Count
        });

        // Sealed product inventory
        var sealedEntries = await _db.SealedInventoryEntries
            .Join(_db.SealedProducts, si => si.ProductIdentifier, sp => sp.Identifier, (si, sp) => new { si, sp })
            .Where(x => x.si.UserId == userId)
            .Select(x => new
            {
                x.si.Quantity,
                x.si.AcquisitionPrice,
                MarketValue = x.sp.CurrentMarketValue
            })
            .ToListAsync(ct);

        decimal sealedValue = sealedEntries.Sum(e => (e.MarketValue ?? 0) * e.Quantity);
        decimal sealedPl = sealedEntries.Sum(e => ((e.MarketValue ?? 0) - e.AcquisitionPrice) * e.Quantity);

        result.SealedProductCount = sealedEntries.Sum(e => e.Quantity);
        result.SealedProductValue = sealedValue;

        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "sealed",
            TotalValue = sealedValue,
            TotalProfitLoss = sealedPl,
            Count = result.SealedProductCount
        });

        result.TotalCardCount = collectionCount;
        result.TotalValue = collectionValue + serializedValue + slabValue + sealedValue;
        result.TotalProfitLoss = collectionPl + serializedPl + slabPl + sealedPl;

        return result;
    }

    public async Task<MetricsResult> GetAggregateMetricsAsync(CollectionFilter filter, CancellationToken ct = default)
    {
        var result = new MetricsResult();

        var collectionQuery = _db.CollectionEntries
            .Join(_db.Cards, ce => ce.CardIdentifier, c => c.Identifier, (ce, c) => new { ce, c });

        if (!string.IsNullOrEmpty(filter.SetCode))
            collectionQuery = collectionQuery.Where(x => x.c.SetCode == filter.SetCode.ToLowerInvariant());
        if (!string.IsNullOrEmpty(filter.Color))
            collectionQuery = collectionQuery.Where(x => x.c.Color == filter.Color);
        if (!string.IsNullOrEmpty(filter.CardType))
            collectionQuery = collectionQuery.Where(x => x.c.CardType != null && x.c.CardType.Contains(filter.CardType));
        if (!string.IsNullOrEmpty(filter.Treatment))
            collectionQuery = collectionQuery.Where(x => x.ce.TreatmentKey == filter.Treatment);
        if (!string.IsNullOrEmpty(filter.Condition) && Enum.TryParse<CardCondition>(filter.Condition, true, out var condFilterAggregate))
            collectionQuery = collectionQuery.Where(x => x.ce.Condition == condFilterAggregate);
        if (filter.Autographed.HasValue)
            collectionQuery = collectionQuery.Where(x => x.ce.Autographed == filter.Autographed.Value);

        var collectionEntries = await collectionQuery
            .Select(x => new
            {
                x.ce.Quantity,
                x.ce.AcquisitionPrice,
                MarketValue = x.c.CurrentMarketValue
            })
            .ToListAsync(ct);

        decimal collectionValue = collectionEntries.Sum(e => (e.MarketValue ?? 0) * e.Quantity);
        decimal collectionPl = collectionEntries.Sum(e => ((e.MarketValue ?? 0) - e.AcquisitionPrice) * e.Quantity);
        int collectionCount = collectionEntries.Sum(e => e.Quantity);

        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "cards",
            TotalValue = collectionValue,
            TotalProfitLoss = collectionPl,
            Count = collectionCount
        });

        // Serialized entries aggregate
        var serializedAgg = await _db.SerializedEntries
            .Join(_db.Cards, se => se.CardIdentifier, c => c.Identifier, (se, c) => new { se, c })
            .Select(x => new { x.se.AcquisitionPrice, MarketValue = x.c.CurrentMarketValue })
            .ToListAsync(ct);

        decimal serializedAggValue = serializedAgg.Sum(e => e.MarketValue ?? 0);
        decimal serializedAggPl = serializedAgg.Sum(e => (e.MarketValue ?? 0) - e.AcquisitionPrice);

        result.SerializedCount = serializedAgg.Count;
        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "serialized",
            TotalValue = serializedAggValue,
            TotalProfitLoss = serializedAggPl,
            Count = result.SerializedCount
        });

        // Slab entries aggregate
        var slabAgg = await _db.SlabEntries
            .Join(_db.Cards, se => se.CardIdentifier, c => c.Identifier, (se, c) => new { se, c })
            .Select(x => new { x.se.AcquisitionPrice, MarketValue = x.c.CurrentMarketValue })
            .ToListAsync(ct);

        decimal slabAggValue = slabAgg.Sum(e => e.MarketValue ?? 0);
        decimal slabAggPl = slabAgg.Sum(e => (e.MarketValue ?? 0) - e.AcquisitionPrice);

        result.SlabCount = slabAgg.Count;
        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "slabs",
            TotalValue = slabAggValue,
            TotalProfitLoss = slabAggPl,
            Count = result.SlabCount
        });

        // Sealed product inventory aggregate
        var sealedAgg = await _db.SealedInventoryEntries
            .Join(_db.SealedProducts, si => si.ProductIdentifier, sp => sp.Identifier, (si, sp) => new { si, sp })
            .Select(x => new { x.si.Quantity, x.si.AcquisitionPrice, MarketValue = x.sp.CurrentMarketValue })
            .ToListAsync(ct);

        decimal sealedAggValue = sealedAgg.Sum(e => (e.MarketValue ?? 0) * e.Quantity);
        decimal sealedAggPl = sealedAgg.Sum(e => ((e.MarketValue ?? 0) - e.AcquisitionPrice) * e.Quantity);
        int sealedCount = sealedAgg.Sum(e => e.Quantity);

        result.SealedProductCount = sealedCount;
        result.SealedProductValue = sealedAggValue;
        result.ByContentType.Add(new ContentTypeBreakdown
        {
            ContentType = "sealed",
            TotalValue = sealedAggValue,
            TotalProfitLoss = sealedAggPl,
            Count = sealedCount
        });

        result.TotalCardCount = collectionCount;
        result.TotalValue = collectionValue + serializedAggValue + slabAggValue + sealedAggValue;
        result.TotalProfitLoss = collectionPl + serializedAggPl + slabAggPl + sealedAggPl;

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

    public async Task<List<SetCompletionResult>> GetAllSetCompletionAsync(Guid userId, bool regularOnly, CancellationToken ct = default)
    {
        var sets = await _db.Sets.ToListAsync(ct);

        var collectionQuery = _db.CollectionEntries.Where(e => e.UserId == userId);
        if (regularOnly)
            collectionQuery = collectionQuery.Where(e => e.TreatmentKey == "regular");

        var allIdentifiers = await collectionQuery
            .Select(e => e.CardIdentifier)
            .Distinct()
            .ToListAsync(ct);

        var results = new List<SetCompletionResult>(sets.Count);
        foreach (var set in sets)
        {
            var ownedCount = allIdentifiers.Count(id => id.StartsWith(set.Code));

            decimal percentage = set.TotalCards > 0
                ? Math.Round((decimal)ownedCount / set.TotalCards * 100, 1)
                : 0;

            results.Add(new SetCompletionResult
            {
                SetCode = set.Code.ToUpperInvariant(),
                SetName = set.Name,
                OwnedCount = ownedCount,
                TotalCards = set.TotalCards,
                Percentage = percentage
            });
        }

        return results.OrderByDescending(r => r.Percentage).ToList();
    }
}
