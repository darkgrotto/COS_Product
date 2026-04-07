using CountOrSell.Data.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace CountOrSell.Api.Services;

public class AvatarService : IAvatarService
{
    private const int MaxDimension = 200;
    private const int MaxUploadBytes = 5 * 1024 * 1024; // 5 MB
    private readonly IImageStore _imageStore;

    // Permitted MIME types and their required magic bytes (offset 0)
    private static readonly IReadOnlyDictionary<string, byte[]> AllowedMagic = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
        ["image/png"]  = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
        ["image/gif"]  = new byte[] { 0x47, 0x49, 0x46, 0x38 },
        ["image/webp"] = new byte[] { 0x52, 0x49, 0x46, 0x46 }, // RIFF header; further bytes checked below
    };

    public AvatarService(IImageStore imageStore)
    {
        _imageStore = imageStore;
    }

    public async Task<(byte[]? jpeg, string? error)> ProcessAvatarAsync(
        Stream input, string contentType, CancellationToken ct)
    {
        // Strip parameters from content type (e.g. "image/jpeg; charset=...")
        var mime = contentType.Split(';')[0].Trim();

        if (!AllowedMagic.TryGetValue(mime, out var expectedMagic))
            return (null, "Unsupported image format. Allowed formats: JPEG, PNG, GIF, WebP.");

        // Read upload into memory with size cap
        using var ms = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0)
        {
            ms.Write(buffer, 0, read);
            if (ms.Length > MaxUploadBytes)
                return (null, "Upload exceeds the 5 MB size limit.");
        }

        var data = ms.ToArray();
        if (data.Length < expectedMagic.Length)
            return (null, "File too small to be a valid image.");

        // Verify magic bytes match declared content type
        for (int i = 0; i < expectedMagic.Length; i++)
        {
            if (data[i] != expectedMagic[i])
                return (null, "File content does not match the declared image format.");
        }

        // Decode, resize, and re-encode as JPEG
        try
        {
            using var image = Image.Load(data);
            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(MaxDimension, MaxDimension),
                    Mode = ResizeMode.Max,
                }));
            }

            using var output = new MemoryStream();
            var encoder = new JpegEncoder { Quality = 85 };
            image.Save(output, encoder);
            return (output.ToArray(), null);
        }
        catch (Exception ex)
        {
            return (null, $"Could not decode image: {ex.Message}");
        }
    }

    public Task<byte[]?> GetAvatarAsync(Guid userId, CancellationToken ct) =>
        _imageStore.GetImageAsync(AvatarPath(userId), ct);

    public Task SaveAvatarAsync(Guid userId, byte[] jpeg, CancellationToken ct) =>
        _imageStore.SaveImageAsync(AvatarPath(userId), jpeg, ct);

    public Task DeleteAvatarAsync(Guid userId, CancellationToken ct) =>
        _imageStore.DeleteImageAsync(AvatarPath(userId), ct);

    public Task<bool> HasAvatarAsync(Guid userId, CancellationToken ct) =>
        _imageStore.ExistsAsync(AvatarPath(userId), ct);

    private static string AvatarPath(Guid userId) => $"avatars/{userId}.jpg";
}
