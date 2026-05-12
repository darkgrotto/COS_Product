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

        var existingCategories = await _db.SealedProductCategories.ToListAsync(ct);
        var existingSubTypes = await _db.SealedProductSubTypes.ToListAsync(ct);

        // Null inventory entries whose category is being removed. The FK cascade
        // would handle this on its own, but doing it here keeps the warning logs
        // attached to the change set.
        var removedCategorySlugs = existingCategories.Select(c => c.Slug)
            .Where(s => !incomingCategorySlugs.Contains(s)).ToList();
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

        // Null inventory entries whose sub-type is being removed (but category remains).
        var removedSubTypeSlugs = existingSubTypes.Select(s => s.Slug)
            .Where(s => !incomingSubTypeSlugs.Contains(s)).ToList();
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

        // Upsert in place. A wholesale delete-and-reinsert triggers the FK
        // SET-NULL cascade on every inventory entry referencing surviving
        // categories - even though those categories are coming back unchanged.
        // SubTypes go first so the category FK stays satisfied through the swap.
        var subTypesToRemove = existingSubTypes.Where(s => !incomingSubTypeSlugs.Contains(s.Slug)).ToList();
        _db.SealedProductSubTypes.RemoveRange(subTypesToRemove);

        var categoriesToRemove = existingCategories.Where(c => !incomingCategorySlugs.Contains(c.Slug)).ToList();
        _db.SealedProductCategories.RemoveRange(categoriesToRemove);

        var existingCategoriesBySlug = existingCategories.ToDictionary(c => c.Slug);
        foreach (var dto in categories)
        {
            if (existingCategoriesBySlug.TryGetValue(dto.Slug, out var existing))
            {
                existing.DisplayName = dto.DisplayName;
                existing.SortOrder = dto.SortOrder;
            }
            else
            {
                _db.SealedProductCategories.Add(new SealedProductCategory
                {
                    Slug = dto.Slug,
                    DisplayName = dto.DisplayName,
                    SortOrder = dto.SortOrder
                });
            }
        }

        var existingSubTypesBySlug = existingSubTypes.ToDictionary(s => s.Slug);
        foreach (var cat in categories)
        {
            foreach (var dto in cat.SubTypes)
            {
                if (existingSubTypesBySlug.TryGetValue(dto.Slug, out var existing))
                {
                    existing.DisplayName = dto.DisplayName;
                    existing.SortOrder = dto.SortOrder;
                    existing.CategorySlug = cat.Slug;
                }
                else
                {
                    _db.SealedProductSubTypes.Add(new SealedProductSubType
                    {
                        Slug = dto.Slug,
                        CategorySlug = cat.Slug,
                        DisplayName = dto.DisplayName,
                        SortOrder = dto.SortOrder
                    });
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
