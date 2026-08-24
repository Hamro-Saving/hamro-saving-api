using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.RemoveAdmin;

internal sealed class RemoveAdminCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<RemoveAdminCommand>
{
    public async Task<Result> Handle(RemoveAdminCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId && m.IsActive, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        var authResult = userContext.EnsureCanAdminister(member.GroupId);
        if (authResult.IsFailure) return authResult;

        if (member.GroupRole != GroupRole.Admin)
            return Result.Failure(UserErrors.Unauthorized);

        // Refuse to leave a group with nobody able to run it.
        bool otherAdminExists = await dbContext.Members
            .AnyAsync(m => m.GroupId == member.GroupId
                        && m.Id != member.Id
                        && m.IsActive
                        && m.GroupRole == GroupRole.Admin, cancellationToken);

        if (!otherAdminExists)
            return Result.Failure(MemberErrors.LastAdmin);

        member.ChangeGroupRole(GroupRole.Member);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
