using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class SealedTaxonomyRepository : ISealedTaxonomyRepository
{
    private readonly AppDbContext _db;
    public SealedTaxonomyRepository(AppDbContext db) => _db = db;

    public Task<List<SealedProductCategory>> GetAllCategoriesAsync(CancellationToken ct = default) =>
        _db.SealedProductCategories.OrderBy(c => c.DisplayName).ToListAsync(ct);

    public Task<List<SealedProductSubType>> GetSubTypesByCategoryAsync(string categorySlug, CancellationToken ct = default) =>
        _db.SealedProductSubTypes
            .Where(s => s.CategorySlug == categorySlug)
            .OrderBy(s => s.DisplayName)
            .ToListAsync(ct);

    public Task<List<SealedProductSubType>> GetAllSubTypesAsync(CancellationToken ct = default) =>
        _db.SealedProductSubTypes.OrderBy(s => s.DisplayName).ToListAsync(ct);
}
