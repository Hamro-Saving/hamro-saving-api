using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HamroSavings.Application.Auth.ForgotPassword;

/// <summary>
/// Succeeds whatever it finds. Anyone at all may post an address here, so answering
/// truthfully — "no such account", or an error only when a send was attempted — would turn
/// this into a way to ask the platform who banks with it. Every caller is told the same
/// thing, and only a real account receives anything.
/// </summary>
internal sealed class ForgotPasswordCommandHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService,
    ILogger<ForgotPasswordCommandHandler> logger)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            logger.LogInformation("Password reset asked for {Email}, which has no account", email);
            return Result.Success();
        }

        // Whether a link may be sent is the domain's to answer, not this handler's: the
        // account must be active, and it must not have been sent one moments ago. The reason
        // goes to the log and no further — telling the caller would say whether the address
        // has an account, and an operator chasing "I never got the email" needs to know
        // which of the two it was.
        var issued = user.GeneratePasswordResetToken(DateTime.UtcNow);

        if (issued.IsFailure)
        {
            logger.LogInformation(
                "No password reset email sent to {Email}: {Reason}", email, issued.Error.Code);
            return Result.Success();
        }

        // The greeting wants a person's name, which lives on their membership rather than
        // their login; someone may hold several, and any of them names the same person.
        var member = await dbContext.Members
            .Where(m => m.UserId == user.Id)
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendPasswordResetAsync(
                new EmailRecipient(user.Email, member?.FullName ?? "there"), issued.Value, cancellationToken);
        }
        catch (Exception exception)
        {
            // Swallowed on purpose: a failure reported back would say "this address has an
            // account" every time the mail server is down. Logged instead, where it is the
            // operator's problem rather than an attacker's answer.
            logger.LogError(exception, "Failed to send password reset email to {Email}", user.Email);
        }

        return Result.Success();
    }
}
