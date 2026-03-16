using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services.Destinations;

// Stub: requires AWSSDK.S3 SDK configuration.
// Implement when AWSSDK.S3 package is added to the project.
public class AwsS3BackupDestination : IBackupDestination
{
    public string DestinationType => "aws-s3";
    public string Label { get; }

    public AwsS3BackupDestination(string label) => Label = label;

    public Task WriteAsync(string fileName, Stream data, CancellationToken ct) =>
        throw new NotImplementedException(
            "AWS S3 destination requires AWSSDK.S3 SDK configuration.");

    public Task<Stream> ReadAsync(string fileName, CancellationToken ct) =>
        throw new NotImplementedException(
            "AWS S3 destination requires AWSSDK.S3 SDK configuration.");

    public Task<List<string>> ListFilesAsync(CancellationToken ct) =>
        Task.FromResult(new List<string>());

    public Task DeleteAsync(string fileName, CancellationToken ct) =>
        throw new NotImplementedException(
            "AWS S3 destination requires AWSSDK.S3 SDK configuration.");

    public Task<bool> TestConnectionAsync(CancellationToken ct) =>
        Task.FromResult(false);
}
