using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ISealedTaxonomyRepository
{
    Task<List<SealedProductCategory>> GetAllCategoriesAsync(CancellationToken ct = default);
    Task<List<SealedProductSubType>> GetSubTypesByCategoryAsync(string categorySlug, CancellationToken ct = default);
    Task<List<SealedProductSubType>> GetAllSubTypesAsync(CancellationToken ct = default);
    Task<List<SealedProductCategoryDto>> GetAllCategoriesWithSubTypesAsync(CancellationToken ct = default);
    Task ReplaceTaxonomyAsync(List<SealedProductCategoryDto> categories, CancellationToken ct = default);
}
