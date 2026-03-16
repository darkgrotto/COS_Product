using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services.Destinations;

// Stub: requires Google.Cloud.Storage.V1 SDK configuration.
// Implement when Google.Cloud.Storage.V1 package is added to the project.
public class GcpStorageBackupDestination : IBackupDestination
{
    public string DestinationType => "gcp-storage";
    public string Label { get; }

    public GcpStorageBackupDestination(string label) => Label = label;

    public Task WriteAsync(string fileName, Stream data, CancellationToken ct) =>
        throw new NotImplementedException(
            "GCP Cloud Storage destination requires Google.Cloud.Storage.V1 SDK configuration.");

    public Task<Stream> ReadAsync(string fileName, CancellationToken ct) =>
        throw new NotImplementedException(
            "GCP Cloud Storage destination requires Google.Cloud.Storage.V1 SDK configuration.");

    public Task<List<string>> ListFilesAsync(CancellationToken ct) =>
        Task.FromResult(new List<string>());

    public Task DeleteAsync(string fileName, CancellationToken ct) =>
        throw new NotImplementedException(
            "GCP Cloud Storage destination requires Google.Cloud.Storage.V1 SDK configuration.");

    public Task<bool> TestConnectionAsync(CancellationToken ct) =>
        Task.FromResult(false);
}
