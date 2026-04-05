using HamroSavings.Application.Abstractions.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace HamroSavings.Infrastructure.Email;

internal sealed class SmtpEmailService(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(
        string recipient,
        string subject,
        string? htmlBody = null,
        string? textBody = null,
        CancellationToken ct = default)
    {
        var host = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        var username = configuration["Email:Username"] ?? throw new InvalidOperationException("Email:Username is not configured.");
        var password = configuration["Email:Password"] ?? throw new InvalidOperationException("Email:Password is not configured.");
        var fromName = configuration["Email:FromName"] ?? "HamroSavings";
        var fromAddress = configuration["Email:FromAddress"] ?? username;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(recipient, recipient));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(username, password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
