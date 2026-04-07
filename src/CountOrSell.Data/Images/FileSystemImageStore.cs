using Microsoft.Extensions.Configuration;

namespace CountOrSell.Data.Images;

public class FileSystemImageStore : IImageStore
{
    private readonly string _basePath;

    public FileSystemImageStore(IConfiguration config)
    {
        _basePath = config["ImageStore:BasePath"] ?? Path.Combine(AppContext.BaseDirectory, "images");
    }

    public async Task SaveImageAsync(string relativePath, byte[] data, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir != null) Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(fullPath, data, ct);
    }

    public async Task<byte[]?> GetImageAsync(string relativePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        if (!File.Exists(fullPath)) return null;
        return await File.ReadAllBytesAsync(fullPath, ct);
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteImageAsync(string relativePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
