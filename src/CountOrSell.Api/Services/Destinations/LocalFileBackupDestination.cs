using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services.Destinations;

public class LocalFileBackupDestination : IBackupDestination
{
    private readonly string _basePath;

    public string DestinationType => "local";
    public string Label { get; }

    public LocalFileBackupDestination(string basePath, string label = "Local")
    {
        _basePath = basePath;
        Label = label;
        Directory.CreateDirectory(basePath);
    }

    public async Task WriteAsync(string fileName, Stream data, CancellationToken ct)
    {
        var path = Path.Combine(_basePath, fileName);
        await using var file = File.Create(path);
        await data.CopyToAsync(file, ct);
    }

    public Task<Stream> ReadAsync(string fileName, CancellationToken ct)
    {
        var path = Path.Combine(_basePath, fileName);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task<List<string>> ListFilesAsync(CancellationToken ct)
    {
        var files = Directory.Exists(_basePath)
            ? Directory.GetFiles(_basePath, "*.zip")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>()
                .ToList()
            : new List<string>();
        return Task.FromResult(files);
    }

    public Task DeleteAsync(string fileName, CancellationToken ct)
    {
        var path = Path.Combine(_basePath, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<bool> TestConnectionAsync(CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(_basePath);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
