using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.Delete;

internal sealed class DeleteMemberCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<DeleteMemberCommand>
{
    public async Task<Result> Handle(DeleteMemberCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        var authResult = userContext.EnsureCanAdminister(member.GroupId);
        if (authResult.IsFailure) return authResult;

        // The login is shared across every group this person belongs to, so it only goes
        // when the last membership does.
        if (member.UserId is not null)
        {
            bool hasOtherMemberships = await dbContext.Members
                .AnyAsync(m => m.UserId == member.UserId && m.Id != member.Id, cancellationToken);

            if (!hasOtherMemberships)
            {
                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == member.UserId, cancellationToken);
                if (user is not null)
                    dbContext.Users.Remove(user);
            }
        }

        dbContext.Members.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
