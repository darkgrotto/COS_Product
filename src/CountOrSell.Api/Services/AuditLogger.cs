using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public class AuditLogger : IAuditLogger
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(IDbContextFactory<AppDbContext> dbFactory, ILogger<AuditLogger> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task LogAsync(
        string actor,
        string actorDisplayName,
        string actionType,
        string? target,
        string result,
        string? ipAddress = null,
        string? sessionId = null)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.AuditLogEntries.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                Actor = actor,
                ActorDisplayName = actorDisplayName,
                ActionType = actionType,
                Target = target,
                Result = result,
                IpAddress = ipAddress,
                SessionId = sessionId
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry: {ActionType} by {Actor}", actionType, actor);
        }
    }
}
