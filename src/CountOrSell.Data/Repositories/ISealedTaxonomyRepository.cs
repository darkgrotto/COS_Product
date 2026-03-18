using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface ISealedTaxonomyRepository
{
    Task<List<SealedProductCategory>> GetAllCategoriesAsync(CancellationToken ct = default);
    Task<List<SealedProductSubType>> GetSubTypesByCategoryAsync(string categorySlug, CancellationToken ct = default);
    Task<List<SealedProductSubType>> GetAllSubTypesAsync(CancellationToken ct = default);
}
