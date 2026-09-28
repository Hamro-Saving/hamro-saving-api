using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Auth.ResetPassword;

/// <summary>
/// Unlike asking for the link, spending one says plainly what went wrong: whoever holds the
/// token was sent it, and a person staring at a dead link needs to know it is dead rather
/// than that their new password was somehow refused.
/// </summary>
internal sealed class ResetPasswordCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == command.Token, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.ResetTokenInvalid);

        if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            return Result.Failure(UserErrors.ResetTokenExpired);

        // Tokens are only handed to active accounts, so this can only be reached by one
        // disabled between asking and answering. Their way back in is not a new password.
        if (!user.IsActive)
            return Result.Failure(UserErrors.NotActive);

        user.ResetPassword(passwordHasher.Hash(command.Password));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
