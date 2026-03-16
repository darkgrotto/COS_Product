namespace CountOrSell.Domain.Models;

// Configured backup destination.
// DestinationType values: "local", "azure-blob", "aws-s3", "gcp-storage"
public class BackupDestinationConfig
{
    public Guid Id { get; set; }
    public string DestinationType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
