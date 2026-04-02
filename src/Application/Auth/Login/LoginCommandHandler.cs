using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Auth.Login;

internal sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider)
    : ICommandHandler<LoginCommand, string>
{
    public async Task<Result<string>> Handle(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);

        if (user is null)
        {
            return Result.Failure<string>(UserErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<string>(UserErrors.NotActive);
        }

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result.Failure<string>(UserErrors.InvalidCredentials);
        }

        var token = tokenProvider.Create(user);
        return Result.Success(token);
    }
}
