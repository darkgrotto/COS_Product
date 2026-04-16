using CountOrSell.Domain.Models;

namespace CountOrSell.Api.Services;

public interface IAuditLogService
{
    Task<List<AuditLogEntry>> GetEntriesAsync(int limit, string? actionType, CancellationToken ct);
    Task<List<string>> GetActionTypesAsync(CancellationToken ct);
}
