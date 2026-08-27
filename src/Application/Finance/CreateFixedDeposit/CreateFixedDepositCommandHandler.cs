using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Ledger;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Groups;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.CreateFixedDeposit;

internal sealed class CreateFixedDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CreateFixedDepositCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFixedDepositCommand command, CancellationToken cancellationToken = default)
    {
        // Admins and members act in the group on their token; only a SuperAdmin names one
        var groupResult = userContext.ResolveWriteGroupId();
        if (groupResult.IsFailure) return Result.Failure<Guid>(groupResult.Error);
        var groupId = groupResult.Value;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<Guid>(GroupErrors.NotFound(groupId));
        }

        var inHand = await CashPosition.InHandAsync(dbContext, groupId, cancellationToken);
        var covered = inHand.EnsureCovers(command.Amount);
        if (covered.IsFailure) return Result.Failure<Guid>(covered.Error);

        var fixedDeposit = FixedDeposit.Create(
            groupId,
            command.InstitutionName,
            command.Amount,
            command.InterestRate,
            command.StartDate,
            command.MaturityDate,
            command.Notes,
            userContext.UserId);

        dbContext.FixedDeposits.Add(fixedDeposit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(fixedDeposit.Id);
    }
}
