namespace CountOrSell.Domain.Services;

public interface IBackupDestination
{
    string DestinationType { get; }
    string Label { get; }
    Task WriteAsync(string fileName, Stream data, CancellationToken ct);
    Task<Stream> ReadAsync(string fileName, CancellationToken ct);
    Task<List<string>> ListFilesAsync(CancellationToken ct);
    Task DeleteAsync(string fileName, CancellationToken ct);
    Task<bool> TestConnectionAsync(CancellationToken ct);
}
