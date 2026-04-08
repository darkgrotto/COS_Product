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

        var allCategories = await _taxonomy.GetAllCategoriesAsync(ct);
        var allSubTypes = await _taxonomy.GetAllSubTypesAsync(ct);

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

        if (request.SubTypeSlug != null && request.CategorySlug == null)
            return BadRequest(new { error = "category_slug is required when sub_type_slug is provided" });

        if (request.CategorySlug != null)
        {
            var categories = await _taxonomy.GetAllCategoriesAsync(ct);
            if (!categories.Any(c => c.Slug == request.CategorySlug))
                return BadRequest(new { error = $"Unknown category slug: {request.CategorySlug}" });
        }

        if (request.SubTypeSlug != null)
        {
            var subTypes = await _taxonomy.GetSubTypesByCategoryAsync(request.CategorySlug!, ct);
            if (!subTypes.Any(s => s.Slug == request.SubTypeSlug))
                return BadRequest(new { error = $"Unknown sub-type slug: {request.SubTypeSlug}" });
        }

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
            CategorySlug = request.CategorySlug,
            SubTypeSlug = request.SubTypeSlug,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _sealedInventory.CreateAsync(entry, ct);
        var allCategories = await _taxonomy.GetAllCategoriesAsync(ct);
        var allSubTypes = await _taxonomy.GetAllSubTypesAsync(ct);
        var categoryMap = allCategories.ToDictionary(c => c.Slug, c => c.DisplayName);
        var subTypeMap = allSubTypes.ToDictionary(s => s.Slug, s => s.DisplayName);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapEntry(created, null, categoryMap, subTypeMap));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _sealedInventory.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId && !IsAdmin) return Forbid();

        var allCategories = await _taxonomy.GetAllCategoriesAsync(ct);
        var allSubTypes = await _taxonomy.GetAllSubTypesAsync(ct);
        var categoryMap = allCategories.ToDictionary(c => c.Slug, c => c.DisplayName);
        var subTypeMap = allSubTypes.ToDictionary(s => s.Slug, s => s.DisplayName);
        return Ok(MapEntry(entry, null, categoryMap, subTypeMap));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SealedInventoryRequest request, CancellationToken ct)
    {
        var entry = await _sealedInventory.GetByIdAsync(id, ct);
        if (entry == null) return NotFound();
        if (entry.UserId != CurrentUserId) return Forbid();

        if (!TryParseCondition(request.Condition, out var condition))
            return BadRequest(new { error = $"Invalid condition: {request.Condition}" });

        if (request.SubTypeSlug != null && request.CategorySlug == null)
            return BadRequest(new { error = "category_slug is required when sub_type_slug is provided" });

        if (request.CategorySlug != null)
        {
            var categories = await _taxonomy.GetAllCategoriesAsync(ct);
            if (!categories.Any(c => c.Slug == request.CategorySlug))
                return BadRequest(new { error = $"Unknown category slug: {request.CategorySlug}" });
        }

        if (request.SubTypeSlug != null)
        {
            var subTypes = await _taxonomy.GetSubTypesByCategoryAsync(request.CategorySlug!, ct);
            if (!subTypes.Any(s => s.Slug == request.SubTypeSlug))
                return BadRequest(new { error = $"Unknown sub-type slug: {request.SubTypeSlug}" });
        }

        entry.Quantity = request.Quantity;
        entry.Condition = condition;
        entry.AcquisitionDate = request.AcquisitionDate;
        entry.AcquisitionPrice = request.AcquisitionPrice;
        entry.Notes = request.Notes;
        entry.CategorySlug = request.CategorySlug;
        entry.SubTypeSlug = request.SubTypeSlug;
        entry.UpdatedAt = DateTime.UtcNow;

        var updated = await _sealedInventory.UpdateAsync(entry, ct);
        var allCategories = await _taxonomy.GetAllCategoriesAsync(ct);
        var allSubTypes = await _taxonomy.GetAllSubTypesAsync(ct);
        var categoryMap = allCategories.ToDictionary(c => c.Slug, c => c.DisplayName);
        var subTypeMap = allSubTypes.ToDictionary(s => s.Slug, s => s.DisplayName);
        return Ok(MapEntry(updated, null, categoryMap, subTypeMap));
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

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkIdsRequest request, CancellationToken ct)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return BadRequest(new { error = "At least one id is required." });
        var deleted = await _sealedInventory.BulkDeleteAsync(request.Ids, CurrentUserId, ct);
        return Ok(new { deleted });
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
        e.CategorySlug,
        CategoryDisplayName = e.CategorySlug != null
            ? categoryMap.GetValueOrDefault(e.CategorySlug)
            : null,
        e.SubTypeSlug,
        SubTypeDisplayName = e.SubTypeSlug != null
            ? subTypeMap.GetValueOrDefault(e.SubTypeSlug)
            : null,
        e.Quantity,
        Condition = e.Condition.ToString(),
        e.AcquisitionDate,
        e.AcquisitionPrice,
        e.Notes,
        CurrentMarketValue = product?.CurrentMarketValue,
        e.CreatedAt,
        e.UpdatedAt
    };
}
