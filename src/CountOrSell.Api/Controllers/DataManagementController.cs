using System.Security.Claims;
using CountOrSell.Api.Filters;
using CountOrSell.Api.Services;
using CountOrSell.Domain.Services;
using CountOrSell.Data;
using CountOrSell.Data.Images;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/admin/data")]
[Authorize(Roles = "Admin")]
public class DataManagementController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IImageStore _imageStore;
    private readonly IAuditLogger _audit;
    private readonly ILogger<DataManagementController> _logger;

    public DataManagementController(
        AppDbContext db,
        IImageStore imageStore,
        IAuditLogger audit,
        ILogger<DataManagementController> logger)
    {
        _db = db;
        _imageStore = imageStore;
        _audit = audit;
        _logger = logger;
    }

    private string ActorName => User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
    private string ActorDisplayName => User.FindFirstValue("display_name") ?? ActorName;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var cardCount = await _db.Cards.CountAsync(ct);
        var setCount = await _db.Sets.CountAsync(ct);
        var sealedCount = await _db.SealedProducts.CountAsync(ct);
        var treatmentCount = await _db.Treatments.CountAsync(ct);

        var imagesBySet = await _imageStore.GetImageCountsBySetAsync(ct);
        var sealedImageCount = await _imageStore.GetSealedImageCountAsync(ct);
        var sealedImagesByProduct = await _imageStore.GetSealedImageCountsByProductAsync(ct);
        var totalImageCount = imagesBySet.Values.Sum() + sealedImageCount;

        // One row per set, carrying metadata and image state together: a set with metadata
        // and no images, and a set with images whose metadata was purged, are both real
        // states an admin needs to see and neither shows up in a per-content-type list.
        var setRows = await _db.Sets
            .Select(x => new { x.Code, x.Name, CardCount = _db.Cards.Count(c => c.SetCode == x.Code) })
            .ToListAsync(ct);

        var known = setRows.Select(r => r.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sets = setRows
            .Select(r => new
            {
                setCode = r.Code,
                name = (string?)r.Name,
                cardCount = r.CardCount,
                imageCount = imagesBySet.TryGetValue(r.Code, out var n) ? n : 0
            })
            // Image directories with no surviving set row - orphaned by a metadata purge.
            // Listing them is the only way an admin can find and clear them.
            .Concat(imagesBySet
                .Where(kv => !known.Contains(kv.Key))
                .Select(kv => new
                {
                    setCode = kv.Key.ToLowerInvariant(),
                    name = (string?)null,
                    cardCount = 0,
                    imageCount = kv.Value
                }))
            .OrderBy(r => r.setCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sealedProducts = await _db.SealedProducts
            .Select(p => new { p.Identifier, p.Name, p.SetCode })
            .ToListAsync(ct);

        return Ok(new
        {
            metadata = new
            {
                setCount,
                cardCount,
                sealedProductCount = sealedCount,
                treatmentCount
            },
            images = new
            {
                totalCount = totalImageCount,
                sealedCount = sealedImageCount,
                bySet = imagesBySet.OrderBy(kv => kv.Key)
                    .Select(kv => new { setCode = kv.Key, count = kv.Value })
            },
            sets,
            sealedProducts = sealedProducts
                .Select(p => new
                {
                    identifier = p.Identifier,
                    name = p.Name,
                    setCode = p.SetCode,
                    imageCount = sealedImagesByProduct.TryGetValue(p.Identifier, out var n) ? n : 0
                })
                .OrderBy(p => p.name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        });
    }

    // ---- Image purge endpoints ----

    [HttpDelete("images/all")]
    [DemoLocked]
    public async Task<IActionResult> PurgeAllImages(CancellationToken ct)
    {
        _logger.LogWarning("Admin {Actor} purging ALL images", ActorName);
        var count = await _imageStore.PurgeAllImagesAsync(ct);
        await _audit.LogAsync(ActorName, ActorDisplayName, "data.purge.images",
            "scope=all", $"Purged {count} images", ClientIp);
        return Ok(new { purged = count, message = $"Purged {count} images." });
    }

    [HttpDelete("images/sealed")]
    [DemoLocked]
    public async Task<IActionResult> PurgeSealedImages(CancellationToken ct)
    {
        _logger.LogWarning("Admin {Actor} purging sealed product images", ActorName);
        var count = await _imageStore.PurgeSealedImagesAsync(ct);
        await _audit.LogAsync(ActorName, ActorDisplayName, "data.purge.images",
            "scope=sealed", $"Purged {count} sealed images", ClientIp);
        return Ok(new { purged = count, message = $"Purged {count} sealed product images." });
    }

    [HttpDelete("images/sets/{setCode}")]
    [DemoLocked]
    public async Task<IActionResult> PurgeSetImages(string setCode, CancellationToken ct)
    {
        var code = setCode.ToLowerInvariant();
        var exists = await _db.Sets.AnyAsync(s => s.Code == code, ct);
        if (!exists) return NotFound(new { error = $"Set '{setCode.ToUpperInvariant()}' not found." });

        _logger.LogWarning("Admin {Actor} purging images for set {SetCode}", ActorName, setCode.ToUpperInvariant());
        var count = await _imageStore.PurgeSetImagesAsync(code, ct);
        await _audit.LogAsync(ActorName, ActorDisplayName, "data.purge.images",
            $"scope=set:{setCode.ToUpperInvariant()}", $"Purged {count} images for set {setCode.ToUpperInvariant()}", ClientIp);
        return Ok(new { purged = count, message = $"Purged {count} images for set {setCode.ToUpperInvariant()}." });
    }

    // ---- Metadata purge endpoints ----

    [HttpDelete("metadata/all")]
    [DemoLocked]
    public async Task<IActionResult> PurgeAllMetadata(CancellationToken ct)
    {
        _logger.LogWarning("Admin {Actor} purging ALL metadata", ActorName);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _db.CardPrices.ExecuteDeleteAsync(ct);
            await _db.CollectionEntries.ExecuteDeleteAsync(ct);
            await _db.WishlistEntries.ExecuteDeleteAsync(ct);
            await _db.SerializedEntries.ExecuteDeleteAsync(ct);
            await _db.Cards.ExecuteDeleteAsync(ct);
            await _db.Sets.ExecuteDeleteAsync(ct);
            await _db.SealedInventoryEntries.ExecuteDeleteAsync(ct);
            await _db.SealedProducts.ExecuteDeleteAsync(ct);
            await _db.SealedProductSubTypes.ExecuteDeleteAsync(ct);
            await _db.SealedProductCategories.ExecuteDeleteAsync(ct);
            await _db.Treatments.ExecuteDeleteAsync(ct);
            await _db.UpdateVersions.ExecuteDeleteAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }

        await _audit.LogAsync(ActorName, ActorDisplayName, "data.purge.metadata",
            "scope=all", "Purged all metadata (sets, cards, sealed products, treatments, update versions)", ClientIp);
        return Ok(new { message = "All metadata purged. Run a content update to restore." });
    }

    [HttpDelete("metadata/sealed")]
    [DemoLocked]
    public async Task<IActionResult> PurgeSealedMetadata(CancellationToken ct)
    {
        _logger.LogWarning("Admin {Actor} purging sealed product metadata", ActorName);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _db.SealedInventoryEntries.ExecuteDeleteAsync(ct);
            await _db.SealedProducts.ExecuteDeleteAsync(ct);
            await _db.SealedProductSubTypes.ExecuteDeleteAsync(ct);
            await _db.SealedProductCategories.ExecuteDeleteAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }

        await _audit.LogAsync(ActorName, ActorDisplayName, "data.purge.metadata",
            "scope=sealed", "Purged sealed product metadata and taxonomy", ClientIp);
        return Ok(new { message = "Sealed product metadata purged. Run a content update to restore." });
    }

    [HttpDelete("metadata/sets/{setCode}")]
    [DemoLocked]
    public async Task<IActionResult> PurgeSetMetadata(string setCode, CancellationToken ct)
    {
        var code = setCode.ToLowerInvariant();
        var exists = await _db.Sets.AnyAsync(s => s.Code == code, ct);
        if (!exists) return NotFound(new { error = $"Set '{setCode.ToUpperInvariant()}' not found." });

        _logger.LogWarning("Admin {Actor} purging metadata for set {SetCode}", ActorName, setCode.ToUpperInvariant());

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var cardIds = await _db.Cards
                .Where(c => c.SetCode == code)
                .Select(c => c.Identifier)
                .ToListAsync(ct);

            if (cardIds.Count > 0)
            {
                await _db.CardPrices.Where(p => cardIds.Contains(p.CardIdentifier)).ExecuteDeleteAsync(ct);
                await _db.CollectionEntries.Where(e => cardIds.Contains(e.CardIdentifier)).ExecuteDeleteAsync(ct);
                await _db.WishlistEntries.Where(e => cardIds.Contains(e.CardIdentifier)).ExecuteDeleteAsync(ct);
                await _db.SerializedEntries.Where(e => cardIds.Contains(e.CardIdentifier)).ExecuteDeleteAsync(ct);
                await _db.Cards.Where(c => c.SetCode == code).ExecuteDeleteAsync(ct);
            }
            await _db.Sets.Where(s => s.Code == code).ExecuteDeleteAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }

        await _audit.LogAsync(ActorName, ActorDisplayName, "data.purge.metadata",
            $"scope=set:{setCode.ToUpperInvariant()}", $"Purged metadata for set {setCode.ToUpperInvariant()}", ClientIp);
        return Ok(new { message = $"Metadata for set {setCode.ToUpperInvariant()} purged. Run a content update to restore." });
    }
}
