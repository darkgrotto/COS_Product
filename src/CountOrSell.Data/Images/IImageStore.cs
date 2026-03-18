namespace CountOrSell.Data.Images;

public interface IImageStore
{
    Task SaveImageAsync(string relativePath, byte[] data, CancellationToken ct);
    Task<byte[]?> GetImageAsync(string relativePath, CancellationToken ct);
    Task<bool> ExistsAsync(string relativePath, CancellationToken ct);
}
