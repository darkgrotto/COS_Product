using System.Security.Claims;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos.Requests;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/sealed-inventory")]
[Authorize]
public class SealedInventoryController : ControllerBase
{
    private readonly ISealedInventoryRepository _sealedInventory;
    private readonly ISealedProductRepository _sealedProducts;
    private readonly ISealedTaxonomyRepository _taxonomy;

    public SealedInventoryController(
        ISealedInventoryRepository sealedInventory,
        ISealedProductRepository sealedProducts,
        ISealedTaxonomyRepository taxonomy)
    {
        _sealedInventory = sealedInventory;
        _sealedProducts = sealedProducts;
        _taxonomy = taxonomy;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin =>
        User.IsInRole("Admin");

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? userId,
        [FromQuery] string? categorySlug,
        [FromQuery] string? subTypeSlug,
        CancellationToken ct)
    {
        if (userId.HasValue && !IsAdmin)
            return Forbid();

        var targetUserId = userId.HasValue ? userId.Value : CurrentUserId;

        List<SealedInventoryEntry> entries;
        if (!string.IsNullOrEmpty(categorySlug) || !string.IsNullOrEmpty(subTypeSlug))
            entries = await _sealedInventory.GetByUserFilteredAsync(targetUserId, categorySlug, subTypeSlug, ct);
        else
            entries = await _sealedInventory.GetByUserAsync(targetUserId, ct);

        var identifiers = entries.Select(e => e.ProductIdentifier).Distinct();
        var products = await _sealedProducts.GetByIdentifiersAsync(identifiers, ct);

        var categorySlugs = products.Values.Select(p => p.CategorySlug).Where(s => s != null).Distinct().ToList();
        var subTypeSlugs = products.Values.Select(p => p.SubTypeSlug).Where(s => s != null).Distinct().ToList();

        var allSubTypes = subTypeSlugs.Count > 0
            ? await _taxonomy.GetAllSubTypesAsync(ct)
            : new List<SealedProductSubType>();
        var allCategories = categorySlugs.Count > 0
            ? await _taxonomy.GetAllCategoriesAsync(ct)
            : new List<SealedProductCategory>();

        var categoryMap = allCategories.ToDictionary(c => c.Slug, c => c.DisplayName);
        var subTypeMap = allSubTypes.ToDictionary(s => s.Slug, s => s.DisplayName);

        return Ok(entries.Select(e =>
        {
            products.TryGetValue(e.ProductIdentifier, out var product);
            return MapEntry(e, product, categoryMap, subTypeMap);
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SealedInventoryRequest request, CancellationToken ct)
    {
        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        var entry = new SealedInventoryEntry
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            ProductIdentifier = request.ProductIdentifier,
            Quantity = request.Quantity,
            Condition = condition,
            AcquisitionDate = request.AcquisitionDate,
            AcquisitionPrice = request.AcquisitionPrice,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _sealedInventory.CreateAsync(entry, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapEntry(created, null, new(), new()));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _sealedInventory.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId && !IsAdmin) return Forbid();
        return Ok(MapEntry(entry, null, new(), new()));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SealedInventoryRequest request, CancellationToken ct)
    {
        var entry = await _sealedInventory.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        entry.Quantity = request.Quantity;
        entry.Condition = condition;
        entry.AcquisitionDate = request.AcquisitionDate;
        entry.AcquisitionPrice = request.AcquisitionPrice;
        entry.Notes = request.Notes;
        entry.UpdatedAt = DateTime.UtcNow;

        var updated = await _sealedInventory.UpdateAsync(entry, ct);
        return Ok(MapEntry(updated, null, new(), new()));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entry = await _sealedInventory.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        await _sealedInventory.DeleteAsync(id, ct);
        return NoContent();
    }

    private static bool TryParseCondition(string value, out CardCondition result) =>
        Enum.TryParse(value, true, out result);

    private static object MapEntry(
        SealedInventoryEntry e,
        SealedProduct? product,
        Dictionary<string, string> categoryMap,
        Dictionary<string, string> subTypeMap) => new
    {
        e.Id,
        e.UserId,
        e.ProductIdentifier,
        ProductName = product?.Name,
        CategoryDisplayName = product?.CategorySlug != null
            ? categoryMap.GetValueOrDefault(product.CategorySlug)
            : null,
        SubTypeDisplayName = product?.SubTypeSlug != null
            ? subTypeMap.GetValueOrDefault(product.SubTypeSlug)
            : null,
        e.Quantity,
        Condition = e.Condition.ToString(),
        e.AcquisitionDate,
        e.AcquisitionPrice,
        e.Notes,
        e.CreatedAt,
        e.UpdatedAt
    };
}
