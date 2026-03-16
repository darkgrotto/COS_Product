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
}
