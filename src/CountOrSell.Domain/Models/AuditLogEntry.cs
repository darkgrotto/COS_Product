namespace CountOrSell.Domain.Models;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? SessionId { get; set; }
}
