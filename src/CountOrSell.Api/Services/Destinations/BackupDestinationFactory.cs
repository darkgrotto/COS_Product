using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services.Destinations;

public class BackupDestinationFactory : IBackupDestinationFactory
{
    private readonly IConfiguration _config;

    public BackupDestinationFactory(IConfiguration config) => _config = config;

    public IBackupDestination Create(BackupDestinationConfig config)
    {
        var opts = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
            config.ConfigurationJson) ?? new Dictionary<string, string>();

        return config.DestinationType switch
        {
            "local" => new LocalFileBackupDestination(
                opts.TryGetValue("path", out var p) ? p
                    : Environment.GetEnvironmentVariable("BACKUP_LOCAL_PATH")
                      ?? "/app/data/backups",
                config.Label),
            "azure-blob" => new AzureBlobBackupDestination(
                opts.TryGetValue("connectionString", out var cs) ? cs
                    : throw new ArgumentException(
                        $"Azure Blob destination '{config.Label}' is missing required "
                        + "'connectionString' in ConfigurationJson."),
                opts.TryGetValue("containerName", out var cn) && !string.IsNullOrWhiteSpace(cn)
                    ? cn : "cos-backups",
                config.Label),
            "aws-s3" => new AwsS3BackupDestination(
                opts.TryGetValue("bucket", out var s3Bucket) ? s3Bucket
                    : throw new ArgumentException(
                        $"AWS S3 destination '{config.Label}' is missing required "
                        + "'bucket' in ConfigurationJson."),
                opts.TryGetValue("region", out var s3Region) ? s3Region
                    : throw new ArgumentException(
                        $"AWS S3 destination '{config.Label}' is missing required "
                        + "'region' in ConfigurationJson."),
                opts.TryGetValue("accessKey", out var s3Ak) ? s3Ak : null,
                opts.TryGetValue("secretKey", out var s3Sk) ? s3Sk : null,
                opts.TryGetValue("serviceUrl", out var s3Url) ? s3Url : null,
                config.Label),
            "gcp-storage" => new GcpStorageBackupDestination(
                opts.TryGetValue("bucket", out var gcsBucket) ? gcsBucket
                    : throw new ArgumentException(
                        $"GCP Storage destination '{config.Label}' is missing required "
                        + "'bucket' in ConfigurationJson."),
                opts.TryGetValue("projectId", out var gcsProject) ? gcsProject
                    : throw new ArgumentException(
                        $"GCP Storage destination '{config.Label}' is missing required "
                        + "'projectId' in ConfigurationJson."),
                opts.TryGetValue("credentialsJson", out var gcsCreds) ? gcsCreds : null,
                opts.TryGetValue("endpoint", out var gcsEndpoint) ? gcsEndpoint : null,
                config.Label),
            _ => throw new ArgumentException($"Unknown destination type: {config.DestinationType}")
        };
    }
}
