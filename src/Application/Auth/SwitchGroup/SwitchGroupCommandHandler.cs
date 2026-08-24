using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Auth.SwitchGroup;

/// <summary>
/// Re-mints the caller's token with a different group active. Since roles are carried in the JWT,
/// this is how a person with several memberships changes which one they are acting under.
/// </summary>
internal sealed class SwitchGroupCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    ITokenProvider tokenProvider)
    : ICommandHandler<SwitchGroupCommand, string>
{
    public async Task<Result<string>> Handle(SwitchGroupCommand command, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
            return Result.Failure<string>(UserErrors.NotFound(userContext.UserId));

        if (!user.IsActive)
            return Result.Failure<string>(UserErrors.NotActive);

        var memberships = await dbContext.LoadMembershipsAsync(user.Id, cancellationToken);

        // Membership is re-read from the database, so a role revoked since login is not carried over.
        var target = memberships.FirstOrDefault(m => m.Group.Id == command.GroupId);

        if (target is null)
            return Result.Failure<string>(UserErrors.NotInGroup);

        var usableResult = MembershipLoader.CheckUsable(target.Group);
        if (usableResult.IsFailure)
            return Result.Failure<string>(usableResult.Error);

        var usable = memberships.Where(m => MembershipLoader.CheckUsable(m.Group).IsSuccess).ToList();

        var token = tokenProvider.Create(user, target.Member, usable.Select(m => m.ToClaim()).ToList());
        return Result.Success(token);
    }
}
