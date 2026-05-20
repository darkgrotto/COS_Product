namespace CountOrSell.Domain.Services;

// Implementations own SDK clients (AmazonS3Client, StorageClient) that themselves
// hold pooled HttpClient/SocketsHttpHandler instances; Dispose forwards to those
// clients so per-call factory instantiations from BackupService and BackupController
// do not slowly leak sockets on long-running containers.
public interface IBackupDestination : IDisposable
{
    string DestinationType { get; }
    string Label { get; }
    Task WriteAsync(string fileName, Stream data, CancellationToken ct);
    Task<Stream> ReadAsync(string fileName, CancellationToken ct);
    Task<List<string>> ListFilesAsync(CancellationToken ct);
    Task DeleteAsync(string fileName, CancellationToken ct);
    Task<bool> TestConnectionAsync(CancellationToken ct);
}
