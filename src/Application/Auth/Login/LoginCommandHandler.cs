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
            return Result.Failure<string>(UserErrors.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<string>(UserErrors.NotActive);

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result.Failure<string>(UserErrors.InvalidCredentials);

        var memberships = await dbContext.LoadMembershipsAsync(user.Id, cancellationToken);

        // Only groups inside their validity window are offered; a person whose one group has
        // expired cannot act, but a SuperAdmin still gets in on their platform role alone.
        var usable = memberships.Where(m => MembershipLoader.CheckUsable(m.Group).IsSuccess).ToList();

        if (usable.Count == 0 && memberships.Count > 0 && !user.IsSuperAdmin)
            return Result.Failure<string>(MembershipLoader.CheckUsable(memberships[0].Group).Error);

        var active = usable.FirstOrDefault();

        var token = tokenProvider.Create(
            user,
            active?.Member,
            usable.Select(m => m.ToClaim()).ToList());

        return Result.Success(token);
    }
}
