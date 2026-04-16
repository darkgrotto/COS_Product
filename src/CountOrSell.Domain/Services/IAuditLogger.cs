namespace CountOrSell.Domain.Services;

public interface IAuditLogger
{
    Task LogAsync(
        string actor,
        string actorDisplayName,
        string actionType,
        string? target,
        string result,
        string? ipAddress = null,
        string? sessionId = null);
}
