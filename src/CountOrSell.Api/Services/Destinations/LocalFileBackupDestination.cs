using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services.Destinations;

public class LocalFileBackupDestination : IBackupDestination
{
    private readonly string _basePath;
    private readonly string _basePathFull;

    public string DestinationType => "local";
    public string Label { get; }

    public LocalFileBackupDestination(string basePath, string label = "Local")
    {
        _basePath = basePath;
        Label = label;
        Directory.CreateDirectory(basePath);
        _basePathFull = Path.GetFullPath(_basePath);
    }

    // Resolves a caller-supplied filename against _basePath and verifies the result is
    // contained within _basePath. Defends every entry point against path-traversal
    // sequences in fileName regardless of caller-side validation.
    private string ResolveSafe(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        var fullPath = Path.GetFullPath(Path.Combine(_basePathFull, fileName));
        if (fullPath != _basePathFull
            && !fullPath.StartsWith(_basePathFull + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Backup file name '{fileName}' escapes the destination base.", nameof(fileName));
        return fullPath;
    }

    public async Task WriteAsync(string fileName, Stream data, CancellationToken ct)
    {
        var path = ResolveSafe(fileName);
        await using var file = File.Create(path);
        await data.CopyToAsync(file, ct);
    }

    public Task<Stream> ReadAsync(string fileName, CancellationToken ct)
    {
        var path = ResolveSafe(fileName);
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
        var path = ResolveSafe(fileName);
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
