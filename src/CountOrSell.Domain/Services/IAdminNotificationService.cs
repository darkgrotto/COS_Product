namespace CountOrSell.Domain.Services;

public interface IAdminNotificationService
{
    Task NotifyAsync(string message, string category, CancellationToken ct);
}
