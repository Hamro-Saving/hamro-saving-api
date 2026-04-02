using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.AssignAdmin;

internal sealed class AssignAdminCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<AssignAdminCommand>
{
    public async Task<Result> Handle(AssignAdminCommand command, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.MemberId && u.IsActive, cancellationToken);

        if (member is null)
        {
            return Result.Failure(UserErrors.NotFound(command.MemberId));
        }

        if (member.Role is not UserRole.Member and not UserRole.Admin)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        if (!member.GroupId.HasValue)
        {
            return Result.Failure(UserErrors.NotInGroup);
        }

        // Demote any existing Admin in this group to Member
        var existingAdmins = await dbContext.Users
            .Where(u => u.GroupId == member.GroupId && u.Role == UserRole.Admin && u.Id != command.MemberId)
            .ToListAsync(cancellationToken);

        foreach (var admin in existingAdmins)
        {
            admin.ChangeRole(UserRole.Member);
        }

        member.ChangeRole(UserRole.Admin);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
