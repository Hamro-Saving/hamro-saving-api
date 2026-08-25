using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Ledger;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.RecordLateJoinerInterest;

/// <summary>
/// Records what a late-joining member paid to catch up with the group. Money coming in,
/// so unlike an expense or a loan there is nothing to check it against — the group is
/// receiving rather than committing.
/// </summary>
internal sealed class RecordLateJoinerInterestCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<RecordLateJoinerInterestCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RecordLateJoinerInterestCommand command, CancellationToken cancellationToken = default)
    {
        var groupResult = userContext.ResolveWriteGroupId();
        if (groupResult.IsFailure) return Result.Failure<Guid>(groupResult.Error);
        var groupId = groupResult.Value;

        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == command.MemberId && m.GroupId == groupId, cancellationToken);

        if (member is null)
            return Result.Failure<Guid>(MemberErrors.NotFound(command.MemberId));

        var recordResult = LateJoinerInterest.Record(
            groupId, command.MemberId, command.Amount, command.PaidDate, command.Notes, userContext.UserId);

        if (recordResult.IsFailure)
            return Result.Failure<Guid>(recordResult.Error);

        var record = recordResult.Value;
        dbContext.LateJoinerInterests.Add(record);

        dbContext.PostLateJoinerInterest(groupId, record.Id, record.MemberId, record.Amount,
            record.PaidDate, $"Late joiner interest from {member.FullName}");

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(record.Id);
    }
}
