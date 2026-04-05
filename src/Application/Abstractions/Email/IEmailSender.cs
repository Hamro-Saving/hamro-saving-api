namespace HamroSavings.Application.Abstractions.Email;

public interface IEmailSender
{
    Task SendAsync(
        string recipient,
        string subject,
        string? htmlBody = null,
        string? textBody = null,
        CancellationToken ct = default);
}
