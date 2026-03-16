using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services.Destinations;

// Stub: requires Azure.Storage.Blobs SDK configuration.
// Implement when Azure.Storage.Blobs package is added to the project.
public class AzureBlobBackupDestination : IBackupDestination
{
    public string DestinationType => "azure-blob";
    public string Label { get; }

    public AzureBlobBackupDestination(string label) => Label = label;

    public Task WriteAsync(string fileName, Stream data, CancellationToken ct) =>
        throw new NotImplementedException(
            "Azure Blob Storage destination requires Azure.Storage.Blobs SDK configuration.");

    public Task<Stream> ReadAsync(string fileName, CancellationToken ct) =>
        throw new NotImplementedException(
            "Azure Blob Storage destination requires Azure.Storage.Blobs SDK configuration.");

    public Task<List<string>> ListFilesAsync(CancellationToken ct) =>
        Task.FromResult(new List<string>());

    public Task DeleteAsync(string fileName, CancellationToken ct) =>
        throw new NotImplementedException(
            "Azure Blob Storage destination requires Azure.Storage.Blobs SDK configuration.");

    public Task<bool> TestConnectionAsync(CancellationToken ct) =>
        Task.FromResult(false);
}
