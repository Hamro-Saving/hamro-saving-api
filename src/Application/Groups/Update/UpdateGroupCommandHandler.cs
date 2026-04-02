using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Groups.Update;

internal sealed class UpdateGroupCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateGroupCommand>
{
    public async Task<Result> Handle(UpdateGroupCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSuperAdmin && userContext.GroupId != command.GroupId)
        {
            return Result.Failure(GroupErrors.NotFound(command.GroupId));
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure(GroupErrors.NotFound(command.GroupId));
        }

        group.Update(command.Name, command.Description, command.MemberInterestRate, command.NonMemberInterestRate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
