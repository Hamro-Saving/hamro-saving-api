using HamroSavings.Application.Abstractions.Email;

namespace HamroSavings.Infrastructure.Email;

internal sealed class EmailService(IEmailSender sender) : IEmailService
{
    public Task SendMemberInviteAsync(string toEmail, string toName, string signupLink, CancellationToken ct = default)
    {
        var html = $"""
            <p>Hello {toName},</p>
            <p>You have been added as a member of a savings group on <strong>HamroSavings</strong>.</p>
            <p>Click the button below to create your account:</p>
            <p>
                <a href="{signupLink}" style="
                    display: inline-block;
                    background-color: #4f46e5;
                    color: white;
                    padding: 12px 24px;
                    text-decoration: none;
                    border-radius: 6px;
                    font-weight: bold;
                ">Create My Account</a>
            </p>
            <p>This link expires in <strong>72 hours</strong>.</p>
            <p>If you did not expect this email, you can safely ignore it.</p>
            <br/>
            <p>— HamroSavings Team</p>
            """;

        var text = $"Hello {toName},\n\nYou have been invited to join HamroSavings. Create your account here:\n{signupLink}\n\nThis link expires in 72 hours.";

        return sender.SendAsync(toEmail, "You've been invited to HamroSavings", html, text, ct);
    }
}
