namespace AgenticPlatform.Core.Interfaces;

public interface ITransactionalEmailService
{
    Task SendConfirmationEmailAsync(
        string recipientEmail,
        string recipientName,
        string confirmationUrl,
        CancellationToken cancellationToken = default);

    Task SendWelcomeGuideAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default);
}
