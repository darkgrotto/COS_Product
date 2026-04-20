using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CountOrSell.Data.Images;

public class FileSystemImageStore : IImageStore
{
    private readonly string _basePath;
    private readonly ILogger<FileSystemImageStore> _logger;

    public FileSystemImageStore(IConfiguration config, ILogger<FileSystemImageStore> logger)
    {
        _basePath = config["ImageStore:BasePath"] ?? Path.Combine(AppContext.BaseDirectory, "images");
        _logger = logger;
        _logger.LogInformation("FileSystemImageStore initialised with basePath: {BasePath}", _basePath);
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
        if (!File.Exists(fullPath))
        {
            _logger.LogDebug("Image not found in store: {FullPath}", fullPath);
            return null;
        }
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

    public Task<bool> HasImagesAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_basePath)) return Task.FromResult(false);
        var hasAny = Directory.EnumerateFiles(_basePath, "*.jpg", SearchOption.AllDirectories).Any();
        return Task.FromResult(hasAny);
    }
}
