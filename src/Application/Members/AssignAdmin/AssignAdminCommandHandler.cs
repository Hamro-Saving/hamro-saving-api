using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.AssignAdmin;

internal sealed class AssignAdminCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<AssignAdminCommand>
{
    public async Task<Result> Handle(AssignAdminCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId && m.IsActive, cancellationToken);

        if (member is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        // NonMembers cannot be assigned as admin
        if (member.MembershipType == MembershipType.NonMember)
            return Result.Failure(UserErrors.Unauthorized);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.MemberId == command.MemberId, cancellationToken);

        if (user is null)
            return Result.Failure(MemberErrors.NotFound(command.MemberId));

        // Demote any existing Admin in this group to Member
        var existingAdminUsers = await dbContext.Users
            .Where(u => u.Role == UserRole.Admin && u.MemberId != null &&
                        dbContext.Members.Any(m => m.Id == u.MemberId && m.GroupId == member.GroupId) &&
                        u.Id != user.Id)
            .ToListAsync(cancellationToken);

        foreach (var admin in existingAdminUsers)
            admin.ChangeRole(UserRole.Member);

        user.ChangeRole(UserRole.Admin);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
