using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Groups.Delete;

internal sealed class DeleteGroupCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteGroupCommand>
{
    public async Task<Result> Handle(DeleteGroupCommand command, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure(GroupErrors.NotFound(command.GroupId));
        }

        bool hasData =
            await dbContext.Users.AnyAsync(u => u.GroupId == command.GroupId, cancellationToken) ||
            await dbContext.Deposits.AnyAsync(d => d.GroupId == command.GroupId, cancellationToken) ||
            await dbContext.Loans.AnyAsync(l => l.GroupId == command.GroupId, cancellationToken) ||
            await dbContext.Expenses.AnyAsync(e => e.GroupId == command.GroupId, cancellationToken) ||
            await dbContext.FixedDeposits.AnyAsync(f => f.GroupId == command.GroupId, cancellationToken);

        if (hasData)
        {
            return Result.Failure(GroupErrors.HasData);
        }

        dbContext.Groups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
