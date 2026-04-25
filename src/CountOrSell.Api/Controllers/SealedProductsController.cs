using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/sealed-products")]
[Authorize]
public class SealedProductsController : ControllerBase
{
    private readonly ISealedProductRepository _products;
    private readonly ISealedTaxonomyRepository _taxonomy;

    public SealedProductsController(
        ISealedProductRepository products,
        ISealedTaxonomyRepository taxonomy)
    {
        _products = products;
        _taxonomy = taxonomy;
    }

    // Returns all sealed products without pagination for admin browsing.
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var all = await _products.GetAllAsync(ct);
        var (categoryMap, subTypeMap) = await LoadTaxonomyMapsAsync(ct);
        return Ok(all.Select(p => MapSummary(p, categoryMap, subTypeMap, includeUpdatedAt: true, includeHasImage: true)));
    }

    [HttpGet("{identifier}")]
    public async Task<IActionResult> GetByIdentifier(string identifier, CancellationToken ct)
    {
        var product = await _products.GetByIdentifierAsync(identifier, ct);
        if (product == null) return NotFound();
        var (categoryMap, subTypeMap) = await LoadTaxonomyMapsAsync(ct);
        return Ok(new
        {
            product.Identifier,
            product.Name,
            SetCode = string.IsNullOrEmpty(product.SetCode) ? (string?)null : product.SetCode.ToUpperInvariant(),
            product.CategorySlug,
            CategoryDisplayName = LookupDisplay(product.CategorySlug, categoryMap),
            product.SubTypeSlug,
            SubTypeDisplayName = LookupDisplay(product.SubTypeSlug, subTypeMap),
            product.CurrentMarketValue,
            product.UpdatedAt,
            ImageUrl = $"/api/images/sealed/{product.Identifier}.jpg",
            SupplementalImageUrl = $"/api/images/sealed/{product.Identifier}_s.jpg"
        });
    }

    [HttpGet]
    public async Task<IActionResult> Browse(
        [FromQuery] string? setCode,
        [FromQuery] string? categorySlug,
        [FromQuery] string? subTypeSlug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var (items, total) = await _products.BrowseAsync(setCode, categorySlug, subTypeSlug, page, pageSize, ct);
        var (categoryMap, subTypeMap) = await LoadTaxonomyMapsAsync(ct);
        return Ok(new
        {
            items = items.Select(p => MapSummary(p, categoryMap, subTypeMap)),
            total,
            page,
            pageSize
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(Array.Empty<object>());

        var results = await _products.SearchAsync(q, ct);
        var (categoryMap, subTypeMap) = await LoadTaxonomyMapsAsync(ct);
        return Ok(results.Select(p => MapSummary(p, categoryMap, subTypeMap)));
    }

    private async Task<(Dictionary<string, string> Categories, Dictionary<string, string> SubTypes)> LoadTaxonomyMapsAsync(CancellationToken ct)
    {
        var categories = await _taxonomy.GetAllCategoriesAsync(ct);
        var subTypes = await _taxonomy.GetAllSubTypesAsync(ct);
        return (
            categories.ToDictionary(c => c.Slug, c => c.DisplayName),
            subTypes.ToDictionary(s => s.Slug, s => s.DisplayName)
        );
    }

    private static string? LookupDisplay(string? slug, Dictionary<string, string> map) =>
        slug != null && map.TryGetValue(slug, out var name) ? name : null;

    private static object MapSummary(
        SealedProduct p,
        Dictionary<string, string> categoryMap,
        Dictionary<string, string> subTypeMap,
        bool includeUpdatedAt = false,
        bool includeHasImage = false)
    {
        var setCode = string.IsNullOrEmpty(p.SetCode) ? (string?)null : p.SetCode.ToUpperInvariant();
        if (includeUpdatedAt && includeHasImage)
        {
            return new
            {
                p.Identifier,
                p.Name,
                SetCode = setCode,
                p.CategorySlug,
                CategoryDisplayName = LookupDisplay(p.CategorySlug, categoryMap),
                p.SubTypeSlug,
                SubTypeDisplayName = LookupDisplay(p.SubTypeSlug, subTypeMap),
                p.CurrentMarketValue,
                p.UpdatedAt,
                HasImage = p.ImagePath != null,
            };
        }
        return new
        {
            p.Identifier,
            p.Name,
            SetCode = setCode,
            p.CategorySlug,
            CategoryDisplayName = LookupDisplay(p.CategorySlug, categoryMap),
            p.SubTypeSlug,
            SubTypeDisplayName = LookupDisplay(p.SubTypeSlug, subTypeMap),
            p.CurrentMarketValue,
            ImageUrl = $"/api/images/sealed/{p.Identifier}.jpg"
        };
    }
}
