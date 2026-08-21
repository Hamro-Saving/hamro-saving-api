using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
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
            return Result.Failure<string>(UserErrors.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<string>(UserErrors.NotActive);

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result.Failure<string>(UserErrors.InvalidCredentials);

        // Look up linked Member to get group context (null for SuperAdmin)
        var member = user.MemberId.HasValue
            ? await dbContext.Members.FindAsync([user.MemberId.Value], cancellationToken)
            : null;

        // For non-SuperAdmin users, validate their group is active and within validity period
        if (member is not null)
        {
            var group = await dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == member.GroupId, cancellationToken);

            if (group is not null)
            {
                if (!group.IsActive)
                    return Result.Failure<string>(GroupErrors.NotActive);

                var now = DateTime.UtcNow;
                if (group.ValidFrom.HasValue && now < group.ValidFrom.Value)
                    return Result.Failure<string>(GroupErrors.ValidityNotStarted);

                if (group.ValidTo.HasValue && now > group.ValidTo.Value)
                    return Result.Failure<string>(GroupErrors.ValidityExpired);
            }
        }

        var token = tokenProvider.Create(user, member);
        return Result.Success(token);
    }
}
