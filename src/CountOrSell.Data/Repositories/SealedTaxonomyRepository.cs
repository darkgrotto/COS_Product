using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CountOrSell.Data.Repositories;

public class SealedTaxonomyRepository : ISealedTaxonomyRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<SealedTaxonomyRepository> _logger;

    public SealedTaxonomyRepository(AppDbContext db, ILogger<SealedTaxonomyRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<List<SealedProductCategory>> GetAllCategoriesAsync(CancellationToken ct = default) =>
        _db.SealedProductCategories.OrderBy(c => c.SortOrder).ToListAsync(ct);

    public Task<List<SealedProductSubType>> GetSubTypesByCategoryAsync(string categorySlug, CancellationToken ct = default) =>
        _db.SealedProductSubTypes
            .Where(s => s.CategorySlug == categorySlug)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

    public Task<List<SealedProductSubType>> GetAllSubTypesAsync(CancellationToken ct = default) =>
        _db.SealedProductSubTypes.OrderBy(s => s.SortOrder).ToListAsync(ct);

    public async Task<List<SealedProductCategoryDto>> GetAllCategoriesWithSubTypesAsync(CancellationToken ct = default)
    {
        var categories = await _db.SealedProductCategories
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        var subTypes = await _db.SealedProductSubTypes
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        var subTypesByCategory = subTypes
            .GroupBy(s => s.CategorySlug)
            .ToDictionary(g => g.Key, g => g.ToList());

        return categories.Select(c => new SealedProductCategoryDto
        {
            Slug = c.Slug,
            DisplayName = c.DisplayName,
            SortOrder = c.SortOrder,
            SubTypes = subTypesByCategory.TryGetValue(c.Slug, out var st)
                ? st.Select(s => new SealedProductSubTypeDto
                {
                    Slug = s.Slug,
                    CategorySlug = s.CategorySlug,
                    DisplayName = s.DisplayName,
                    SortOrder = s.SortOrder
                }).ToList()
                : new List<SealedProductSubTypeDto>()
        }).ToList();
    }

    public async Task ReplaceTaxonomyAsync(List<SealedProductCategoryDto> categories, CancellationToken ct = default)
    {
        var incomingCategorySlugs = categories.Select(c => c.Slug).ToHashSet();
        var incomingSubTypeSlugs = categories.SelectMany(c => c.SubTypes).Select(s => s.Slug).ToHashSet();

        // Null orphaned inventory entries where category is being removed
        var existingCategorySlugs = await _db.SealedProductCategories.Select(c => c.Slug).ToListAsync(ct);
        var removedCategorySlugs = existingCategorySlugs.Where(s => !incomingCategorySlugs.Contains(s)).ToList();
        if (removedCategorySlugs.Count > 0)
        {
            var orphanedByCategory = await _db.SealedInventoryEntries
                .Where(e => e.CategorySlug != null && removedCategorySlugs.Contains(e.CategorySlug!))
                .ToListAsync(ct);
            foreach (var entry in orphanedByCategory)
            {
                _logger.LogWarning(
                    "Nulling category and sub-type on sealed inventory entry {Id} (product: {Product}) - category slug {Slug} removed from taxonomy",
                    entry.Id, entry.ProductIdentifier, entry.CategorySlug);
                entry.CategorySlug = null;
                entry.SubTypeSlug = null;
            }
        }

        // Null orphaned inventory entries where sub-type is being removed (but category remains)
        var existingSubTypeSlugs = await _db.SealedProductSubTypes.Select(s => s.Slug).ToListAsync(ct);
        var removedSubTypeSlugs = existingSubTypeSlugs.Where(s => !incomingSubTypeSlugs.Contains(s)).ToList();
        if (removedSubTypeSlugs.Count > 0)
        {
            var orphanedBySubType = await _db.SealedInventoryEntries
                .Where(e => e.SubTypeSlug != null && removedSubTypeSlugs.Contains(e.SubTypeSlug!))
                .ToListAsync(ct);
            foreach (var entry in orphanedBySubType)
            {
                _logger.LogWarning(
                    "Nulling sub-type on sealed inventory entry {Id} (product: {Product}) - sub-type slug {Slug} removed from taxonomy",
                    entry.Id, entry.ProductIdentifier, entry.SubTypeSlug);
                entry.SubTypeSlug = null;
            }
        }

        // Full replace: delete all existing taxonomy rows, then insert incoming
        var allCategories = await _db.SealedProductCategories.ToListAsync(ct);
        var allSubTypes = await _db.SealedProductSubTypes.ToListAsync(ct);

        _db.SealedProductSubTypes.RemoveRange(allSubTypes);
        _db.SealedProductCategories.RemoveRange(allCategories);
        await _db.SaveChangesAsync(ct);

        var newCategories = categories.Select(c => new SealedProductCategory
        {
            Slug = c.Slug,
            DisplayName = c.DisplayName,
            SortOrder = c.SortOrder
        }).ToList();

        var newSubTypes = categories.SelectMany(c => c.SubTypes.Select(s => new SealedProductSubType
        {
            Slug = s.Slug,
            CategorySlug = c.Slug,
            DisplayName = s.DisplayName,
            SortOrder = s.SortOrder
        })).ToList();

        _db.SealedProductCategories.AddRange(newCategories);
        _db.SealedProductSubTypes.AddRange(newSubTypes);
        await _db.SaveChangesAsync(ct);
    }
}
