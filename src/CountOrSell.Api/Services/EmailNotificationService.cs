using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(ILogger<EmailNotificationService> logger) => _logger = logger;

    public Task SendUpdateNotificationAsync(string subject, string body, CancellationToken ct)
    {
        // Stub: email notification implementation is provider-specific and not yet implemented
        _logger.LogInformation("Email notification stub - Subject: {Subject}", subject);
        return Task.CompletedTask;
    }

    public Task SendInvitationAsync(string toEmail, string inviteUrl, CancellationToken ct)
    {
        // Stub: email notification implementation is provider-specific and not yet implemented
        _logger.LogInformation("Invitation email stub - To: {Email}, Url: {Url}", toEmail, inviteUrl);
        return Task.CompletedTask;
    }
}
