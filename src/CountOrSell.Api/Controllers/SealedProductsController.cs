using CountOrSell.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/sealed-products")]
[Authorize]
public class SealedProductsController : ControllerBase
{
    private readonly ISealedProductRepository _products;

    public SealedProductsController(ISealedProductRepository products)
    {
        _products = products;
    }

    // Returns all sealed products without pagination for admin browsing.
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var all = await _products.GetAllAsync(ct);
        return Ok(all.Select(p => new
        {
            p.Identifier,
            p.Name,
            SetCode = string.IsNullOrEmpty(p.SetCode) ? (string?)null : p.SetCode.ToUpperInvariant(),
            p.CategorySlug,
            p.SubTypeSlug,
            p.CurrentMarketValue,
            p.UpdatedAt,
            HasImage = p.ImagePath != null,
        }));
    }

    [HttpGet("{identifier}")]
    public async Task<IActionResult> GetByIdentifier(string identifier, CancellationToken ct)
    {
        var product = await _products.GetByIdentifierAsync(identifier, ct);
        if (product == null) return NotFound();
        return Ok(new
        {
            product.Identifier,
            product.Name,
            SetCode = string.IsNullOrEmpty(product.SetCode) ? (string?)null : product.SetCode.ToUpperInvariant(),
            product.CategorySlug,
            product.SubTypeSlug,
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
        return Ok(new
        {
            items = items.Select(p => new
            {
                p.Identifier,
                p.Name,
                SetCode = string.IsNullOrEmpty(p.SetCode) ? (string?)null : p.SetCode.ToUpperInvariant(),
                p.CategorySlug,
                p.SubTypeSlug,
                p.CurrentMarketValue,
                ImageUrl = $"/api/images/sealed/{p.Identifier}.jpg"
            }),
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
        return Ok(results.Select(p => new
        {
            p.Identifier,
            p.Name,
            SetCode = p.SetCode.ToUpperInvariant(),
            p.CategorySlug,
            p.SubTypeSlug,
            p.CurrentMarketValue,
            ImageUrl = $"/api/images/sealed/{p.Identifier}.jpg"
        }));
    }
}
