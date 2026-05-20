using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services.Destinations;

public class AzureBlobBackupDestination : IBackupDestination
{
    private readonly BlobContainerClient _container;

    public string DestinationType => "azure-blob";
    public string Label { get; }

    public AzureBlobBackupDestination(string connectionString, string containerName, string label)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(
                "Azure Blob connection string is required.", nameof(connectionString));
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException(
                "Azure Blob container name is required.", nameof(containerName));

        _container = new BlobContainerClient(connectionString, containerName);
        Label = label;
    }

    public async Task WriteAsync(string fileName, Stream data, CancellationToken ct)
    {
        ValidateFileName(fileName);
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        var blob = _container.GetBlobClient(fileName);
        await blob.UploadAsync(data, overwrite: true, cancellationToken: ct);
    }

    public async Task<Stream> ReadAsync(string fileName, CancellationToken ct)
    {
        ValidateFileName(fileName);
        var blob = _container.GetBlobClient(fileName);
        var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task<List<string>> ListFilesAsync(CancellationToken ct)
    {
        var files = new List<string>();
        if (!await _container.ExistsAsync(ct))
            return files;

        await foreach (var item in _container.GetBlobsAsync(cancellationToken: ct))
        {
            if (item.Name.EndsWith(".zip", StringComparison.Ordinal))
                files.Add(item.Name);
        }
        return files;
    }

    public async Task DeleteAsync(string fileName, CancellationToken ct)
    {
        ValidateFileName(fileName);
        var blob = _container.GetBlobClient(fileName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct)
    {
        try
        {
            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            return true;
        }
        catch (RequestFailedException)
        {
            return false;
        }
    }

    public void Dispose() { /* BlobContainerClient does not implement IDisposable */ }

    // Defends against blob path-traversal patterns and absolute paths in caller-supplied
    // names. Azure treats forward slashes as virtual directories, so we require a plain
    // file name matching the BackupFileName format used by BackupService.
    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            throw new ArgumentException(
                $"Blob name '{fileName}' must not contain path separators or '..'.",
                nameof(fileName));
    }
}
