using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Auth.Signup;

internal sealed class SignupWithTokenCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider)
    : ICommandHandler<SignupWithTokenCommand, string>
{
    public async Task<Result<string>> Handle(SignupWithTokenCommand command, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.InviteToken == command.Token, cancellationToken);

        if (user is null)
            return Result.Failure<string>(UserErrors.InviteTokenInvalid);

        if (user.InviteTokenExpiresAt < DateTime.UtcNow)
            return Result.Failure<string>(UserErrors.InviteTokenExpired);

        if (user.IsActive)
            return Result.Failure<string>(UserErrors.AlreadyActivated);

        var passwordHash = passwordHasher.Hash(command.Password);
        user.AcceptInvite(passwordHash);

        await dbContext.SaveChangesAsync(cancellationToken);

        var member = user.MemberId.HasValue
            ? await dbContext.Members.FindAsync([user.MemberId.Value], cancellationToken)
            : null;

        var token = tokenProvider.Create(user, member);
        return Result.Success(token);
    }
}
