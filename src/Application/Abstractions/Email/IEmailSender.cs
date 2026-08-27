namespace HamroSavings.Application.Abstractions.Email;

public interface IEmailSender
{
    /// <param name="fromName">
    /// The name the message appears to come from — the group's, so a person who belongs to
    /// several can tell at a glance which one is writing. Only the display name varies; the
    /// address behind it is the one mailbox the server authenticates as.
    /// </param>
    Task SendAsync(
        string recipient,
        string fromName,
        string subject,
        string? htmlBody = null,
        string? textBody = null,
        CancellationToken ct = default);
}
