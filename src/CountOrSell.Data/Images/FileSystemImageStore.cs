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

    public Task<int> PurgeSetImagesAsync(string setCode, CancellationToken ct)
    {
        var setPath = Path.Combine(_basePath, "sets", setCode.ToLowerInvariant());
        if (!Directory.Exists(setPath)) return Task.FromResult(0);
        var files = Directory.GetFiles(setPath, "*.jpg");
        foreach (var f in files) File.Delete(f);
        _logger.LogInformation("Purged {Count} images for set {SetCode}", files.Length, setCode.ToUpperInvariant());
        return Task.FromResult(files.Length);
    }

    public Task<int> PurgeSealedImagesAsync(CancellationToken ct)
    {
        var sealedPath = Path.Combine(_basePath, "sealed");
        if (!Directory.Exists(sealedPath)) return Task.FromResult(0);
        var files = Directory.GetFiles(sealedPath, "*.jpg");
        foreach (var f in files) File.Delete(f);
        _logger.LogInformation("Purged {Count} sealed product images", files.Length);
        return Task.FromResult(files.Length);
    }

    public Task<int> PurgeAllImagesAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_basePath)) return Task.FromResult(0);
        var files = Directory.GetFiles(_basePath, "*.jpg", SearchOption.AllDirectories).ToArray();
        foreach (var f in files) File.Delete(f);
        _logger.LogInformation("Purged all {Count} images", files.Length);
        return Task.FromResult(files.Length);
    }

    public Task<Dictionary<string, int>> GetImageCountsBySetAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var setsPath = Path.Combine(_basePath, "sets");
        if (!Directory.Exists(setsPath)) return Task.FromResult(result);
        foreach (var dir in Directory.GetDirectories(setsPath))
        {
            var setCode = Path.GetFileName(dir).ToUpperInvariant();
            result[setCode] = Directory.GetFiles(dir, "*.jpg").Length;
        }
        return Task.FromResult(result);
    }

    public Task<int> GetSealedImageCountAsync(CancellationToken ct)
    {
        var sealedPath = Path.Combine(_basePath, "sealed");
        if (!Directory.Exists(sealedPath)) return Task.FromResult(0);
        return Task.FromResult(Directory.GetFiles(sealedPath, "*.jpg").Length);
    }
}
