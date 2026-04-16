using CountOrSell.Data;
using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AuditLogEntry>> GetEntriesAsync(int limit, string? actionType, CancellationToken ct)
    {
        var query = _db.AuditLogEntries.AsQueryable();
        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(e => e.ActionType == actionType);
        return await query
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetActionTypesAsync(CancellationToken ct)
    {
        return await _db.AuditLogEntries
            .Select(e => e.ActionType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);
    }
}
