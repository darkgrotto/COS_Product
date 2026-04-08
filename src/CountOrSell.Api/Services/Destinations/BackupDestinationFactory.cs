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
            "azure-blob" => new AzureBlobBackupDestination(config.Label),
            "aws-s3" => new AwsS3BackupDestination(config.Label),
            "gcp-storage" => new GcpStorageBackupDestination(config.Label),
            _ => throw new ArgumentException($"Unknown destination type: {config.DestinationType}")
        };
    }
}
