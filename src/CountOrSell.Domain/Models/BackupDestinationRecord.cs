namespace CountOrSell.Domain.Models;

public class BackupDestinationRecord
{
    public Guid Id { get; set; }
    public Guid BackupRecordId { get; set; }
    public string DestinationType { get; set; } = string.Empty;
    public string DestinationLabel { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAt { get; set; }
}
