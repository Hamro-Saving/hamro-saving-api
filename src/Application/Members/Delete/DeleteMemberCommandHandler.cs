using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.Delete;

internal sealed class DeleteMemberCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteMemberCommand>
{
    public async Task<Result> Handle(DeleteMemberCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        // Remove linked User if one exists
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.MemberId == command.MemberId, cancellationToken);
        if (user is not null)
            dbContext.Users.Remove(user);

        dbContext.Members.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
