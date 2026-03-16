using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class AdminNotificationService : IAdminNotificationService
{
    private readonly AppDbContext _db;

    public AdminNotificationService(AppDbContext db) => _db = db;

    public async Task NotifyAsync(string message, string category, CancellationToken ct)
    {
        _db.AdminNotifications.Add(new AdminNotification
        {
            Message = message,
            Category = category,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
