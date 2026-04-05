namespace HamroSavings.Application.Abstractions.Email;

public interface IEmailService
{
    Task SendMemberInviteAsync(string toEmail, string toName, string signupLink, CancellationToken ct = default);
}
