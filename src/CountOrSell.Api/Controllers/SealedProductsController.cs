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
