using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Groups.Create;

internal sealed class CreateGroupCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<CreateGroupCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGroupCommand command, CancellationToken cancellationToken = default)
    {
        bool codeExists = await dbContext.Groups
            .AnyAsync(g => g.Code == command.Code.ToUpperInvariant(), cancellationToken);

        if (codeExists)
        {
            return Result.Failure<Guid>(GroupErrors.CodeNotUnique);
        }

        var group = Group.Create(
            command.Name,
            command.Code,
            command.Description,
            command.MemberInterestRate,
            command.NonMemberInterestRate);

        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(group.Id);
    }
}
