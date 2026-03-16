using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Domain.Models;

public class BackupRecord
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public BackupType BackupType { get; set; }
    public int SchemaVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public bool IsAvailable { get; set; }
    public List<BackupDestinationRecord> Destinations { get; set; } = new();
}
