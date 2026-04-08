using CountOrSell.Data.Images;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/images")]
[Authorize]
public class ImagesController : ControllerBase
{
    private readonly IImageStore _imageStore;
    private readonly ICardImageFetcher _imageFetcher;
    private readonly ILogger<ImagesController> _logger;

    // Set code: 3-4 lowercase alphanumeric characters
    private static readonly System.Text.RegularExpressions.Regex SetCodeRegex =
        new(@"^[a-z0-9]{3,4}$",
            System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    // Card image filename is {identifier}.jpg e.g. "eoe019.jpg"
    private static readonly System.Text.RegularExpressions.Regex CardFileNameRegex =
        new(@"^[a-z0-9]{3,4}\d{3,4}[a-z]?\.(jpg|jpeg|png|webp)$",
            System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    // Sealed product image filename: alphanumeric, hyphens, underscores + extension
    private static readonly System.Text.RegularExpressions.Regex SealedFileNameRegex =
        new(@"^[a-z0-9][a-z0-9\-_]*(_s)?\.(jpg|jpeg|png|webp)$",
            System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public ImagesController(
        IImageStore imageStore,
        ICardImageFetcher imageFetcher,
        ILogger<ImagesController> logger)
    {
        _imageStore = imageStore;
        _imageFetcher = imageFetcher;
        _logger = logger;
    }

    [HttpGet("cards/{setCode}/{fileName}")]
    public async Task<IActionResult> GetCardImage(string setCode, string fileName, CancellationToken ct)
    {
        if (!SetCodeRegex.IsMatch(setCode) || !CardFileNameRegex.IsMatch(fileName))
            return BadRequest();

        var relativePath = Path.Combine("sets", setCode.ToLowerInvariant(), fileName.ToLowerInvariant());

        var data = await _imageStore.GetImageAsync(relativePath, ct);

        if (data == null)
        {
            // Fallback: fetch from Scryfall and cache locally
            var identifier = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
            data = await _imageFetcher.FetchAsync(identifier, ct);

            if (data != null)
            {
                try
                {
                    await _imageStore.SaveImageAsync(relativePath, data, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache fetched image for {Identifier}", identifier);
                }
            }
        }

        if (data == null)
            return NotFound();

        var contentType = fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
            : fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
            : "image/jpeg";

        return File(data, contentType);
    }

    [HttpGet("sealed/{fileName}")]
    public async Task<IActionResult> GetSealedImage(string fileName, CancellationToken ct)
    {
        if (!SealedFileNameRegex.IsMatch(fileName))
            return BadRequest();

        var relativePath = Path.Combine("sealed", fileName);
        var data = await _imageStore.GetImageAsync(relativePath, ct);

        if (data == null)
            return NotFound();

        var contentType = fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
            : fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
            : "image/jpeg";

        return File(data, contentType);
    }
}
