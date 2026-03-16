namespace CountOrSell.Data.Images;

public interface IImageStore
{
    Task SaveImageAsync(string relativePath, byte[] data, CancellationToken ct);
}
