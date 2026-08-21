using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Groups.SetValidity;

internal sealed class SetGroupValidityCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<SetGroupValidityCommand>
{
    public async Task<Result> Handle(SetGroupValidityCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSuperAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
            return Result.Failure(GroupErrors.NotFound(command.GroupId));

        group.SetValidity(command.IsActive, command.ValidFrom, command.ValidTo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
