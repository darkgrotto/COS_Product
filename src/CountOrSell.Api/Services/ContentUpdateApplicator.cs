using System.IO.Compression;
using System.Text.Json;
using CountOrSell.Data;
using CountOrSell.Data.Images;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

// Applies a content update package (ZIP) to the database.
//
// ZIP structure (from countorsell.com packages):
//   manifest.json                               - per-package manifest (already parsed, passed in)
//   metadata/treatments.json                    - array of TreatmentDto
//   metadata/taxonomy.json                      - TaxonomyDto (categories + sub_types)
//   metadata/sets/{set_code}/set.json           - SetDto (one per set)
//   metadata/sets/{set_code}/cards.json         - array of CardDto (one per set)
//   metadata/sealed/{product_id}.json           - SealedProductDto (one per product)
//   images/sets/{set_code}/{card_id}.jpg        - card images
//   images/sealed/{product_id}.jpg              - sealed product front image
//   images/sealed/{product_id}_s.jpg            - sealed product supplemental image
//
// Checksums in the per-package manifest use format "sha256:<hex_lowercase>" per file path.
// Image files are saved outside the DB transaction - image failures are non-fatal.
public class ContentUpdateApplicator : IContentUpdateApplicator
{
    private readonly AppDbContext _db;
    private readonly IImageStore _imageStore;
    private readonly ISealedTaxonomyRepository _taxonomy;
    private readonly IPackageVerifier _verifier;
    private readonly ILogger<ContentUpdateApplicator> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ContentUpdateApplicator(
        AppDbContext db,
        IImageStore imageStore,
        ISealedTaxonomyRepository taxonomy,
        IPackageVerifier verifier,
        ILogger<ContentUpdateApplicator> logger)
    {
        _db = db;
        _imageStore = imageStore;
        _taxonomy = taxonomy;
        _verifier = verifier;
        _logger = logger;
    }

    public async Task ApplyContentUpdateAsync(
        Stream packageStream, PackageManifest packageManifest, CancellationToken ct)
    {
        if (packageStream.CanSeek) packageStream.Position = 0;
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true);

        // Determine content version from package manifest (cards is the primary version key)
        var contentVersion = packageManifest.ContentVersions.TryGetValue("cards", out var cv)
            ? cv.Version
            : packageManifest.GeneratedAt.ToString("yyyy-MM-dd");

        // Read and verify metadata files
        var treatments = ReadAndVerifyJson<List<TreatmentDto>>(
            archive, "metadata/treatments.json", packageManifest.Checksums);

        var taxonomy = ReadAndVerifyJson<TaxonomyDto>(
            archive, "metadata/taxonomy.json", packageManifest.Checksums);

        // Collect all sets, cards, pricing, and sealed products across per-set directories
        var allSets = new List<SetDto>();
        var allCards = new List<CardDto>();
        var allPricing = new List<PricingEntryDto>();
        var allSealedProducts = new List<SealedProductDto>();

        foreach (var entry in archive.Entries)
        {
            if (entry.Name.Length == 0) continue; // directory entry

            if (entry.FullName.StartsWith("metadata/sets/", StringComparison.OrdinalIgnoreCase))
            {
                if (entry.Name.Equals("set.json", StringComparison.OrdinalIgnoreCase))
                {
                    var set = ReadAndVerifyJson<SetDto>(
                        archive, entry.FullName, packageManifest.Checksums);
                    if (set != null) allSets.Add(set);
                }
                else if (entry.Name.Equals("cards.json", StringComparison.OrdinalIgnoreCase))
                {
                    var cards = ReadAndVerifyJson<List<CardDto>>(
                        archive, entry.FullName, packageManifest.Checksums);
                    if (cards != null) allCards.AddRange(cards);
                }
                else if (entry.Name.Equals("pricing.json", StringComparison.OrdinalIgnoreCase))
                {
                    var pricing = ReadAndVerifyJson<List<PricingEntryDto>>(
                        archive, entry.FullName, packageManifest.Checksums);
                    if (pricing != null) allPricing.AddRange(pricing);
                }
            }
            else if (entry.FullName.StartsWith("metadata/sealed/", StringComparison.OrdinalIgnoreCase)
                     && entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var product = ReadAndVerifyJson<SealedProductDto>(
                    archive, entry.FullName, packageManifest.Checksums);
                if (product != null) allSealedProducts.Add(product);
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            if (treatments != null) await UpsertTreatmentsAsync(treatments, ct);
            if (allSets.Count > 0) await UpsertSetsAsync(allSets, ct);
            if (allCards.Count > 0) await UpsertCardsAsync(allCards, ct);
            if (allPricing.Count > 0) await UpsertCardPricesAsync(allPricing, ct);

            // Taxonomy replace must happen before sealed products (FK dependency)
            if (taxonomy != null)
                await _taxonomy.ReplaceTaxonomyAsync(FlattenCategories(taxonomy), ct);

            if (allSealedProducts.Count > 0)
                await UpsertSealedProductsAsync(allSealedProducts, ct);

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

        // Save images outside the transaction - best effort, non-fatal
        await SaveImagesAsync(archive, packageManifest.Checksums, ct);
    }

    private T? ReadAndVerifyJson<T>(
        ZipArchive archive, string entryPath, Dictionary<string, string> checksums)
    {
        var entry = archive.GetEntry(entryPath);
        if (entry == null) return default;

        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();

        if (checksums.TryGetValue(entryPath, out var expectedChecksum))
        {
            if (!_verifier.VerifyFileChecksum(bytes, expectedChecksum))
            {
                _logger.LogError("Checksum mismatch for {Path}", entryPath);
                throw new InvalidDataException($"Checksum mismatch for {entryPath}");
            }
        }

        var json = System.Text.Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private static List<SealedProductCategoryDto> FlattenCategories(TaxonomyDto taxonomy)
    {
        foreach (var cat in taxonomy.Categories)
            foreach (var sub in cat.SubTypes)
                sub.CategorySlug = cat.Slug;
        return taxonomy.Categories;
    }

    private async Task UpsertTreatmentsAsync(List<TreatmentDto> dtos, CancellationToken ct)
    {
        var existingKeys = await _db.Treatments.Select(t => t.Key).ToListAsync(ct);
        _db.Treatments.AddRange(dtos
            .Where(d => !existingKeys.Contains(d.Key))
            .Select(d => new Treatment { Key = d.Key, DisplayName = d.DisplayName, SortOrder = d.SortOrder }));

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
        _db.Sets.AddRange(dtos
            .Where(d => !existingCodes.Contains(d.Code))
            .Select(d => new Set
            {
                Code = d.Code,
                Name = d.Name,
                TotalCards = d.TotalCards,
                SetType = d.SetType,
                ReleaseDate = d.ReleaseDate,
                Digital = false,
                UpdatedAt = DateTime.UtcNow
            }));

        foreach (var dto in dtos.Where(d => existingCodes.Contains(d.Code)))
        {
            var entity = await _db.Sets.FindAsync(new object[] { dto.Code }, ct);
            if (entity != null)
            {
                entity.Name = dto.Name;
                entity.TotalCards = dto.TotalCards;
                entity.SetType = dto.SetType;
                entity.ReleaseDate = dto.ReleaseDate;
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertCardsAsync(List<CardDto> dtos, CancellationToken ct)
    {
        var existingIds = await _db.Cards.Select(c => c.Identifier).ToListAsync(ct);
        _db.Cards.AddRange(dtos
            .Where(d => !existingIds.Contains(d.Identifier))
            .Select(d => new Card
            {
                Identifier = d.Identifier,
                SetCode = d.SetCode,
                Name = d.Name,
                Color = d.Colors.Count > 0 ? string.Join(",", d.Colors) : null,
                CardType = d.TypeLine,
                Rarity = d.Rarity,
                IsReserved = d.IsReserved,
                OracleRulingUrl = d.OracleRulingUri,
                UpdatedAt = DateTime.UtcNow
            }));

        foreach (var dto in dtos.Where(d => existingIds.Contains(d.Identifier)))
        {
            var entity = await _db.Cards.FindAsync(new object[] { dto.Identifier }, ct);
            if (entity != null)
            {
                entity.SetCode = dto.SetCode;
                entity.Name = dto.Name;
                entity.Color = dto.Colors.Count > 0 ? string.Join(",", dto.Colors) : null;
                entity.CardType = dto.TypeLine;
                entity.Rarity = dto.Rarity;
                entity.IsReserved = dto.IsReserved;
                entity.OracleRulingUrl = dto.OracleRulingUri;
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertCardPricesAsync(List<PricingEntryDto> dtos, CancellationToken ct)
    {
        var identifiers = dtos.Select(d => d.CardIdentifier).Distinct().ToList();
        var existing = await _db.CardPrices
            .Where(p => identifiers.Contains(p.CardIdentifier))
            .ToListAsync(ct);
        var existingMap = existing.ToDictionary(p => (p.CardIdentifier, p.TreatmentKey));

        foreach (var dto in dtos)
        {
            var key = (dto.CardIdentifier, dto.TreatmentKey);
            if (existingMap.TryGetValue(key, out var entity))
            {
                entity.PriceUsd = dto.PriceUsd;
                entity.CapturedAt = dto.CapturedAt;
            }
            else
            {
                _db.CardPrices.Add(new CardPrice
                {
                    CardIdentifier = dto.CardIdentifier,
                    TreatmentKey = dto.TreatmentKey,
                    PriceUsd = dto.PriceUsd,
                    CapturedAt = dto.CapturedAt
                });
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertSealedProductsAsync(List<SealedProductDto> dtos, CancellationToken ct)
    {
        var existingIds = await _db.SealedProducts.Select(s => s.Identifier).ToListAsync(ct);
        _db.SealedProducts.AddRange(dtos
            .Where(d => !existingIds.Contains(d.Identifier))
            .Select(d => new SealedProduct
            {
                Identifier = d.Identifier,
                SetCode = d.SetCode ?? string.Empty,
                Name = d.Name,
                CategorySlug = d.CategorySlug,
                SubTypeSlug = d.SubTypeSlug,
                UpdatedAt = DateTime.UtcNow
            }));

        foreach (var dto in dtos.Where(d => existingIds.Contains(d.Identifier)))
        {
            var entity = await _db.SealedProducts.FindAsync(new object[] { dto.Identifier }, ct);
            if (entity != null)
            {
                entity.SetCode = dto.SetCode ?? string.Empty;
                entity.Name = dto.Name;
                entity.CategorySlug = dto.CategorySlug;
                entity.SubTypeSlug = dto.SubTypeSlug;
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task SaveImagesAsync(
        ZipArchive archive, Dictionary<string, string> checksums, CancellationToken ct)
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
                var bytes = ms.ToArray();

                if (checksums.TryGetValue(entry.FullName, out var expectedChecksum)
                    && !_verifier.VerifyFileChecksum(bytes, expectedChecksum))
                {
                    _logger.LogWarning("Checksum mismatch for image {Path}, skipping", entry.FullName);
                    continue;
                }

                await _imageStore.SaveImageAsync(entry.FullName, bytes, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save image {Path}", entry.FullName);
            }
        }
    }
}
