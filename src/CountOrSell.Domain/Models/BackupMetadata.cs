namespace CountOrSell.Domain.Models;

// Serialized into backup archive as metadata.json
// BackupType values: "scheduled" or "pre-update"
public class BackupMetadata
{
    public int BackupFormatVersion { get; set; } = 1;
    public string InstanceName { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string BackupType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Label { get; set; } = string.Empty;
}
