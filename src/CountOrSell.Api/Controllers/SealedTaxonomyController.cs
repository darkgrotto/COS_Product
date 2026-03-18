using CountOrSell.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/sealed-taxonomy")]
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
        var categories = await _taxonomy.GetAllCategoriesAsync(ct);
        return Ok(categories.Select(c => new { c.Slug, c.DisplayName }));
    }

    [HttpGet("sub-types")]
    public async Task<IActionResult> GetSubTypes([FromQuery] string? categorySlug, CancellationToken ct)
    {
        var subTypes = string.IsNullOrEmpty(categorySlug)
            ? await _taxonomy.GetAllSubTypesAsync(ct)
            : await _taxonomy.GetSubTypesByCategoryAsync(categorySlug, ct);

        return Ok(subTypes.Select(s => new { s.Slug, s.CategorySlug, s.DisplayName }));
    }
}
