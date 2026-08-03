namespace CountOrSell.Domain.Services;

public interface IAdminNotificationService
{
    Task NotifyAsync(string message, string category, CancellationToken ct);

    // Like NotifyAsync, but skips creation when an unread notification with the
    // same message and category already exists. Use for externally triggerable
    // events (e.g. repeated OAuth sign-in attempts) so they cannot flood the
    // notification list.
    Task NotifyOnceAsync(string message, string category, CancellationToken ct);
}
