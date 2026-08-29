using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.UpdateOtherIncomingFund;

/// <summary>
/// Corrects a receipt before it is verified. The member it was recorded against is not
/// changeable here: money in from someone else is a different receipt, not a correction of
/// this one.
/// </summary>
internal sealed class UpdateOtherIncomingFundCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateOtherIncomingFundCommand>
{
    public async Task<Result> Handle(UpdateOtherIncomingFundCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var record = await dbContext.OtherIncomingFunds
            .FirstOrDefaultAsync(r => r.Id == command.RecordId, cancellationToken);

        if (record is null)
            return Result.Failure(OtherIncomingFundErrors.NotFound(command.RecordId));

        if (!userContext.CanWrite(record.GroupId))
            return Result.Failure(OtherIncomingFundErrors.NotInGroup);

        var result = record.Update(command.Amount, command.PaidDate, command.Remarks);
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
