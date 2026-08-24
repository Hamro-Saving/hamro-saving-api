using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.AssignAdmin;

/// <summary>
/// Promotes one membership to group admin. A group admin may do this inside their own group; a
/// SuperAdmin may do it anywhere, which is how a newly created group gets its first admin.
/// </summary>
internal sealed class AssignAdminCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<AssignAdminCommand>
{
    public async Task<Result> Handle(AssignAdminCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId && m.IsActive, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        var authResult = userContext.EnsureCanAdminister(member.GroupId);
        if (authResult.IsFailure) return authResult;

        // A NonMember is a borrower, not a participant, so cannot run the group.
        if (member.GroupRole == GroupRole.NonMember)
            return Result.Failure(UserErrors.Unauthorized);

        // Admin duties need a login to act through.
        if (member.UserId is null)
            return Result.Failure(MemberErrors.NoLinkedUser(member.Id));

        if (member.GroupRole == GroupRole.Admin)
            return Result.Success();

        member.ChangeGroupRole(GroupRole.Admin);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
