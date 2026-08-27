using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Ledger;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.VerifyOtherIncomingFund;

internal sealed class VerifyOtherIncomingFundCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<VerifyOtherIncomingFundCommand>
{
    public async Task<Result> Handle(VerifyOtherIncomingFundCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var record = await dbContext.OtherIncomingFunds
            .FirstOrDefaultAsync(r => r.Id == command.RecordId, cancellationToken);

        if (record is null)
            return Result.Failure(OtherIncomingFundErrors.NotFound(command.RecordId));

        if (!userContext.CanWrite(record.GroupId))
            return Result.Failure(OtherIncomingFundErrors.NotInGroup);

        var member = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == record.MemberId, cancellationToken);

        var result = record.Verify(userContext.UserId);
        if (result.IsFailure) return result;

        dbContext.PostOtherIncome(record.GroupId, record.Id, record.MemberId, record.Amount,
            record.PaidDate, $"Late joiner interest from {member?.FullName ?? "member"}");

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
