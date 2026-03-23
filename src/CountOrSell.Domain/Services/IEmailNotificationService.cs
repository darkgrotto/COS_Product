namespace CountOrSell.Domain.Services;

// Stub interface - email notification implementation detail is provider-specific.
public interface IEmailNotificationService
{
    Task SendUpdateNotificationAsync(string subject, string body, CancellationToken ct);
    Task SendInvitationAsync(string toEmail, string inviteUrl, CancellationToken ct);
}
