using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

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

    public async Task NotifyOnceAsync(string message, string category, CancellationToken ct)
    {
        var alreadyPending = await _db.AdminNotifications
            .AnyAsync(n => !n.IsRead && n.Category == category && n.Message == message, ct);
        if (alreadyPending)
            return;

        await NotifyAsync(message, category, ct);
    }
}
