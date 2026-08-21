using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.RemoveAdmin;

internal sealed class RemoveAdminCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<RemoveAdminCommand>
{
    public async Task<Result> Handle(RemoveAdminCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId && m.IsActive, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.MemberId == command.MemberId, cancellationToken);

        if (user is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        if (user.Role != UserRole.Admin)
            return Result.Failure(UserErrors.Unauthorized);

        user.ChangeRole(UserRole.Member);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
