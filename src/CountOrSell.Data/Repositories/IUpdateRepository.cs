using CountOrSell.Domain.Models;

namespace CountOrSell.Data.Repositories;

public interface IUpdateRepository
{
    Task<string?> GetCurrentContentVersionAsync(CancellationToken ct);
    Task<int> GetCurrentSchemaVersionAsync(CancellationToken ct);
    Task<PendingSchemaUpdate?> GetPendingSchemaUpdateAsync(CancellationToken ct);
    Task AddPendingSchemaUpdateAsync(PendingSchemaUpdate update, CancellationToken ct);
    Task ApprovePendingSchemaUpdateAsync(int id, Guid approvedByUserId, CancellationToken ct);
    Task<List<AdminNotification>> GetUnreadNotificationsAsync(CancellationToken ct);
    Task MarkNotificationReadAsync(int id, CancellationToken ct);
    Task MarkAllNotificationsReadAsync(CancellationToken ct);
    Task<string?> GetLatestApplicationVersionAsync(CancellationToken ct);
    Task SetLatestApplicationVersionAsync(string version, CancellationToken ct);
    Task<DateTime?> GetLastUpdateCheckedAtAsync(CancellationToken ct);
    Task SetLastUpdateCheckedAtAsync(DateTime checkedAt, CancellationToken ct);
}
