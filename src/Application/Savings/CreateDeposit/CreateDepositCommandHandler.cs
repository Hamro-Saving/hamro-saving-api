using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Members;
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
        // Admins and members act in the group on their token; only a SuperAdmin names one
        var groupResult = userContext.ResolveGroupId(command.GroupId);
        if (groupResult.IsFailure) return Result.Failure<Guid>(groupResult.Error);
        var groupId = groupResult.Value;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<Guid>(GroupErrors.NotFound(groupId));
        }

        var memberExists = await dbContext.Members
            .AnyAsync(m => m.Id == command.MemberId && m.GroupId == groupId, cancellationToken);

        if (!memberExists)
        {
            return Result.Failure<Guid>(MemberErrors.NotFound(command.MemberId));
        }

        var deposit = Deposit.Create(
            command.MemberId,
            groupId,
            command.Amount,
            command.DepositMonth,
            command.DepositYear,
            command.DepositDate,
            command.Type,
            command.Notes,
            userContext.UserId);

        dbContext.Deposits.Add(deposit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(deposit.Id);
    }
}
