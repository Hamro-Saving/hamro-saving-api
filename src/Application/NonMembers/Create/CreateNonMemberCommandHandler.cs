using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.NonMembers.Create;

internal sealed class CreateNonMemberCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CreateNonMemberCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateNonMemberCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSuperAdmin && userContext.GroupId != command.GroupId)
        {
            return Result.Failure<Guid>(UserErrors.NotInGroup);
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<Guid>(GroupErrors.NotFound(command.GroupId));
        }

        var nonMember = NonMember.Create(
            command.FullName,
            command.Email,
            command.Phone,
            command.Address,
            command.GroupId);

        dbContext.NonMembers.Add(nonMember);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(nonMember.Id);
    }
}
