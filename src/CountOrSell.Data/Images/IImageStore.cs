namespace CountOrSell.Data.Images;

public interface IImageStore
{
    Task SaveImageAsync(string relativePath, byte[] data, CancellationToken ct);
    Task<byte[]?> GetImageAsync(string relativePath, CancellationToken ct);
    Task<bool> ExistsAsync(string relativePath, CancellationToken ct);
    Task DeleteImageAsync(string relativePath, CancellationToken ct);
    Task<bool> HasImagesAsync(CancellationToken ct);

    // Bulk operations - return number of files affected
    Task<int> PurgeSetImagesAsync(string setCode, CancellationToken ct);
    Task<int> PurgeSealedImagesAsync(CancellationToken ct);
    Task<int> PurgeAllImagesAsync(CancellationToken ct);
    Task<Dictionary<string, int>> GetImageCountsBySetAsync(CancellationToken ct);
    Task<int> GetSealedImageCountAsync(CancellationToken ct);
}
