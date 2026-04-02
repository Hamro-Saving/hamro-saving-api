using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Savings;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Savings.CreateDeposit;

internal sealed class CreateDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CreateDepositCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDepositCommand command, CancellationToken cancellationToken = default)
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

        var memberExists = await dbContext.Users
            .AnyAsync(u => u.Id == command.MemberId && u.Role == UserRole.Member && u.GroupId == command.GroupId, cancellationToken);

        if (!memberExists)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(command.MemberId));
        }

        var deposit = Deposit.Create(
            command.MemberId,
            command.GroupId,
            command.Amount,
            command.DepositMonth,
            command.DepositYear,
            command.Type,
            command.Notes,
            userContext.UserId);

        dbContext.Deposits.Add(deposit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(deposit.Id);
    }
}
