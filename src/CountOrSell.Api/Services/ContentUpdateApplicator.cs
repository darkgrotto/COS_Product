using System.IO.Compression;
using System.Text.Json;
using CountOrSell.Data;
using CountOrSell.Data.Images;
using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public class ContentUpdateApplicator : IContentUpdateApplicator
{
    private readonly AppDbContext _db;
    private readonly IImageStore _imageStore;
    private readonly ILogger<ContentUpdateApplicator> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ContentUpdateApplicator(
        AppDbContext db,
        IImageStore imageStore,
        ILogger<ContentUpdateApplicator> logger)
    {
        _db = db;
        _imageStore = imageStore;
        _logger = logger;
    }

    public async Task ApplyContentUpdateAsync(
        Stream packageStream, string contentVersion, CancellationToken ct)
    {
        if (packageStream.CanSeek) packageStream.Position = 0;

        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true);

        var treatments = ReadJsonEntry<List<TreatmentDto>>(archive, "treatments.json");
        var sets = ReadJsonEntry<List<SetDto>>(archive, "sets.json");
        var cards = ReadJsonEntry<List<CardDto>>(archive, "cards.json");
        var sealedCategories = ReadJsonEntry<List<SealedProductCategoryDto>>(archive, "sealed_product_categories.json");
        var sealedSubTypes = ReadJsonEntry<List<SealedProductSubTypeDto>>(archive, "sealed_product_sub_types.json");
        var sealedProducts = ReadJsonEntry<List<SealedProductDto>>(archive, "sealed_products.json");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            if (treatments != null) await UpsertTreatmentsAsync(treatments, ct);
            if (sets != null) await UpsertSetsAsync(sets, ct);
            if (cards != null) await UpsertCardsAsync(cards, ct);
            // Taxonomy must be upserted before sealed products (FK dependency)
            if (sealedCategories != null) await UpsertSealedProductCategoriesAsync(sealedCategories, ct);
            if (sealedSubTypes != null) await UpsertSealedProductSubTypesAsync(sealedSubTypes, ct);
            if (sealedProducts != null) await UpsertSealedProductsAsync(sealedProducts, ct);

            _db.UpdateVersions.Add(new UpdateVersion
            {
                ContentVersion = contentVersion,
                AppliedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        // Save images outside the transaction - best effort
        await SaveImagesAsync(archive, ct);
    }

    private static T? ReadJsonEntry<T>(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        if (entry == null) return default;
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private async Task UpsertTreatmentsAsync(List<TreatmentDto> dtos, CancellationToken ct)
    {
        var existingKeys = await _db.Treatments.Select(t => t.Key).ToListAsync(ct);
        var toAdd = dtos.Where(d => !existingKeys.Contains(d.Key))
            .Select(d => new Treatment { Key = d.Key, DisplayName = d.DisplayName, SortOrder = d.SortOrder });
        _db.Treatments.AddRange(toAdd);
        foreach (var dto in dtos.Where(d => existingKeys.Contains(d.Key)))
        {
            var entity = await _db.Treatments.FindAsync(new object[] { dto.Key }, ct);
            if (entity != null)
            {
                entity.DisplayName = dto.DisplayName;
                entity.SortOrder = dto.SortOrder;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertSetsAsync(List<SetDto> dtos, CancellationToken ct)
    {
        var existingCodes = await _db.Sets.Select(s => s.Code).ToListAsync(ct);
        var toAdd = dtos.Where(d => !existingCodes.Contains(d.Code))
            .Select(d => new Set
            {
                Code = d.Code,
                Name = d.Name,
                TotalCards = d.TotalCards,
                ReleaseDate = d.ReleaseDate,
                UpdatedAt = DateTime.UtcNow
            });
        _db.Sets.AddRange(toAdd);
        foreach (var dto in dtos.Where(d => existingCodes.Contains(d.Code)))
        {
            var entity = await _db.Sets.FindAsync(new object[] { dto.Code }, ct);
            if (entity != null)
            {
                entity.Name = dto.Name;
                entity.TotalCards = dto.TotalCards;
                entity.ReleaseDate = dto.ReleaseDate;
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertCardsAsync(List<CardDto> dtos, CancellationToken ct)
    {
        var existingIds = await _db.Cards.Select(c => c.Identifier).ToListAsync(ct);
        var toAdd = dtos.Where(d => !existingIds.Contains(d.Identifier))
            .Select(d => new Card
            {
                Identifier = d.Identifier,
                SetCode = d.SetCode,
                Name = d.Name,
                Color = d.Color,
                CardType = d.CardType,
                CurrentMarketValue = d.MarketValue,
                IsReserved = d.IsReserved,
                OracleRulingUrl = d.OracleRulingUrl,
                UpdatedAt = DateTime.UtcNow
            });
        _db.Cards.AddRange(toAdd);
        foreach (var dto in dtos.Where(d => existingIds.Contains(d.Identifier)))
        {
            var entity = await _db.Cards.FindAsync(new object[] { dto.Identifier }, ct);
            if (entity != null)
            {
                entity.SetCode = dto.SetCode;
                entity.Name = dto.Name;
                entity.Color = dto.Color;
                entity.CardType = dto.CardType;
                entity.CurrentMarketValue = dto.MarketValue;
                entity.IsReserved = dto.IsReserved;
                entity.OracleRulingUrl = dto.OracleRulingUrl;
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertSealedProductCategoriesAsync(List<SealedProductCategoryDto> dtos, CancellationToken ct)
    {
        var existingSlugs = await _db.SealedProductCategories.Select(c => c.Slug).ToListAsync(ct);
        var toAdd = dtos.Where(d => !existingSlugs.Contains(d.Slug))
            .Select(d => new SealedProductCategory { Slug = d.Slug, DisplayName = d.DisplayName });
        _db.SealedProductCategories.AddRange(toAdd);
        foreach (var dto in dtos.Where(d => existingSlugs.Contains(d.Slug)))
        {
            var entity = await _db.SealedProductCategories.FindAsync(new object[] { dto.Slug }, ct);
            if (entity != null)
                entity.DisplayName = dto.DisplayName;
        }
        // Null out orphaned references for any slugs removed from the package
        var incomingSlugs = dtos.Select(d => d.Slug).ToHashSet();
        var removedSlugs = existingSlugs.Where(s => !incomingSlugs.Contains(s)).ToList();
        if (removedSlugs.Count > 0)
        {
            var orphaned = await _db.SealedProducts
                .Where(p => p.CategorySlug != null && removedSlugs.Contains(p.CategorySlug!))
                .ToListAsync(ct);
            foreach (var p in orphaned)
            {
                p.CategorySlug = null;
                p.SubTypeSlug = null; // sub-type is implicitly invalid without its category
            }
            _db.SealedProductCategories.RemoveRange(
                await _db.SealedProductCategories
                    .Where(c => removedSlugs.Contains(c.Slug))
                    .ToListAsync(ct));
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertSealedProductSubTypesAsync(List<SealedProductSubTypeDto> dtos, CancellationToken ct)
    {
        var existingSlugs = await _db.SealedProductSubTypes.Select(s => s.Slug).ToListAsync(ct);
        var toAdd = dtos.Where(d => !existingSlugs.Contains(d.Slug))
            .Select(d => new SealedProductSubType
            {
                Slug = d.Slug,
                CategorySlug = d.CategorySlug,
                DisplayName = d.DisplayName
            });
        _db.SealedProductSubTypes.AddRange(toAdd);
        foreach (var dto in dtos.Where(d => existingSlugs.Contains(d.Slug)))
        {
            var entity = await _db.SealedProductSubTypes.FindAsync(new object[] { dto.Slug }, ct);
            if (entity != null)
            {
                entity.CategorySlug = dto.CategorySlug;
                entity.DisplayName = dto.DisplayName;
            }
        }
        // Null out orphaned sub-type references for removed slugs
        var incomingSlugs = dtos.Select(d => d.Slug).ToHashSet();
        var removedSlugs = existingSlugs.Where(s => !incomingSlugs.Contains(s)).ToList();
        if (removedSlugs.Count > 0)
        {
            var orphaned = await _db.SealedProducts
                .Where(p => p.SubTypeSlug != null && removedSlugs.Contains(p.SubTypeSlug!))
                .ToListAsync(ct);
            foreach (var p in orphaned)
                p.SubTypeSlug = null;
            _db.SealedProductSubTypes.RemoveRange(
                await _db.SealedProductSubTypes
                    .Where(s => removedSlugs.Contains(s.Slug))
                    .ToListAsync(ct));
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertSealedProductsAsync(List<SealedProductDto> dtos, CancellationToken ct)
    {
        var existingIds = await _db.SealedProducts.Select(s => s.Identifier).ToListAsync(ct);
        var toAdd = dtos.Where(d => !existingIds.Contains(d.Identifier))
            .Select(d => new SealedProduct
            {
                Identifier = d.Identifier,
                SetCode = d.SetCode,
                Name = d.Name,
                CategorySlug = d.CategorySlug,
                SubTypeSlug = d.SubTypeSlug,
                CurrentMarketValue = d.CurrentMarketValue,
                UpdatedAt = DateTime.UtcNow
            });
        _db.SealedProducts.AddRange(toAdd);
        foreach (var dto in dtos.Where(d => existingIds.Contains(d.Identifier)))
        {
            var entity = await _db.SealedProducts.FindAsync(new object[] { dto.Identifier }, ct);
            if (entity != null)
            {
                entity.SetCode = dto.SetCode;
                entity.Name = dto.Name;
                entity.CategorySlug = dto.CategorySlug;
                entity.SubTypeSlug = dto.SubTypeSlug;
                entity.CurrentMarketValue = dto.CurrentMarketValue;
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task SaveImagesAsync(ZipArchive archive, CancellationToken ct)
    {
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith("images/", StringComparison.OrdinalIgnoreCase)) continue;
            if (entry.Name.Length == 0) continue; // directory entry

            try
            {
                using var stream = entry.Open();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                await _imageStore.SaveImageAsync(entry.FullName, ms.ToArray(), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save image {Path}", entry.FullName);
            }
        }
    }
}
