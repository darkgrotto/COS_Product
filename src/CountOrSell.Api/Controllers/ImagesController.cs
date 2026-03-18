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

    // Image filename is {identifier}.jpg e.g. "eoe019.jpg"
    private static readonly System.Text.RegularExpressions.Regex FileNameRegex =
        new(@"^[a-z0-9]{3,4}\d{3,4}\.(jpg|jpeg|png|webp)$",
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

    [HttpGet("cards/{fileName}")]
    public async Task<IActionResult> GetCardImage(string fileName, CancellationToken ct)
    {
        if (!FileNameRegex.IsMatch(fileName))
            return BadRequest();

        var relativePath = Path.Combine("cards", fileName);

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
}
