namespace CountOrSell.Api.Services;

public interface IImageStatsService
{
    Task<(int CardImages, int SealedImages)> GetCountsAsync(CancellationToken ct = default);
    void Invalidate();
}

// Counts JPEG files under the configured image store. Recursive directory walks
// are slow against large image trees, so the result is cached for TtlSeconds and
// recomputed lazily after expiry. Update package application should call
// Invalidate() so admin dashboards reflect freshly downloaded images.
public sealed class ImageStatsService : IImageStatsService
{
    private const int TtlSeconds = 300;

    private readonly IConfiguration _config;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private (int CardImages, int SealedImages)? _cached;
    private DateTime _cachedAt;

    public ImageStatsService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<(int CardImages, int SealedImages)> GetCountsAsync(CancellationToken ct = default)
    {
        if (_cached.HasValue && (DateTime.UtcNow - _cachedAt).TotalSeconds < TtlSeconds)
            return _cached.Value;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cached.HasValue && (DateTime.UtcNow - _cachedAt).TotalSeconds < TtlSeconds)
                return _cached.Value;

            var counts = await Task.Run(CountFiles, ct);
            _cached = counts;
            _cachedAt = DateTime.UtcNow;
            return counts;
        }
        finally { _lock.Release(); }
    }

    public void Invalidate()
    {
        _lock.Wait();
        try { _cached = null; }
        finally { _lock.Release(); }
    }

    private (int CardImages, int SealedImages) CountFiles()
    {
        var basePath = _config["ImageStore:BasePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "images");
        if (!Directory.Exists(basePath)) return (0, 0);

        var setsPath = Path.Combine(basePath, "sets");
        var sealedPath = Path.Combine(basePath, "sealed");

        var cardImages = Directory.Exists(setsPath)
            ? Directory.GetFiles(setsPath, "*.jpg", SearchOption.AllDirectories).Length
            : 0;
        var sealedImages = Directory.Exists(sealedPath)
            ? Directory.GetFiles(sealedPath, "*.jpg", SearchOption.AllDirectories).Length
            : 0;

        return (cardImages, sealedImages);
    }
}
