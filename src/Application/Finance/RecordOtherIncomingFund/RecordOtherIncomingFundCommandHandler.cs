using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Ledger;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.RecordOtherIncomingFund;

/// <summary>
/// Records what a late-joining member paid to catch up. No cash rule applies — the group is
/// receiving, not committing.
/// </summary>
internal sealed class RecordOtherIncomingFundCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<RecordOtherIncomingFundCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RecordOtherIncomingFundCommand command, CancellationToken cancellationToken = default)
    {
        var groupResult = userContext.ResolveWriteGroupId();
        if (groupResult.IsFailure) return Result.Failure<Guid>(groupResult.Error);
        var groupId = groupResult.Value;

        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId && m.GroupId == groupId, cancellationToken);

        if (member is null)
            return Result.Failure<Guid>(MemberErrors.NotFound(command.MemberId));

        var recordResult = OtherIncomingFund.Record(
            groupId, command.MemberId, command.Amount, command.PaidDate, command.Remarks, userContext.UserId);

        if (recordResult.IsFailure)
            return Result.Failure<Guid>(recordResult.Error);

        var record = recordResult.Value;
        dbContext.OtherIncomingFunds.Add(record);

        // Posted to the ledger only once an admin has verified it.
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(record.Id);
    }
}
