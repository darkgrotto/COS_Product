using CountOrSell.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/sealed-product-taxonomy")]
[Authorize]
public class SealedTaxonomyController : ControllerBase
{
    private readonly ISealedTaxonomyRepository _taxonomy;

    public SealedTaxonomyController(ISealedTaxonomyRepository taxonomy)
    {
        _taxonomy = taxonomy;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _taxonomy.GetAllCategoriesWithSubTypesAsync(ct);
        return Ok(categories.Select(c => new
        {
            c.Slug,
            c.DisplayName,
            c.SortOrder,
            SubTypes = c.SubTypes.Select(s => new
            {
                s.Slug,
                s.CategorySlug,
                s.DisplayName,
                s.SortOrder
            })
        }));
    }
}
