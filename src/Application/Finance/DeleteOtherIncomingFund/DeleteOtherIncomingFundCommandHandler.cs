using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.DeleteOtherIncomingFund;

/// <summary>
/// Removes a receipt entered in error. Only while unverified: once verified it is income on
/// the group's books, and a ledger entry is never unwritten — correcting that is an opposite
/// entry, not a deletion.
/// </summary>
internal sealed class DeleteOtherIncomingFundCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<DeleteOtherIncomingFundCommand>
{
    public async Task<Result> Handle(DeleteOtherIncomingFundCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var record = await dbContext.OtherIncomingFunds
            .FirstOrDefaultAsync(r => r.Id == command.RecordId, cancellationToken);

        if (record is null)
            return Result.Failure(OtherIncomingFundErrors.NotFound(command.RecordId));

        if (!userContext.CanWrite(record.GroupId))
            return Result.Failure(OtherIncomingFundErrors.NotInGroup);

        if (record.IsVerified)
            return Result.Failure(OtherIncomingFundErrors.CannotModifyVerified);

        // Belt and braces: an unverified receipt should have no entries, and if one somehow
        // exists then deleting the record would orphan the books.
        var inLedger = await dbContext.LedgerEntries
            .AnyAsync(e => e.SourceType == "OtherIncomingFund" && e.SourceId == record.Id, cancellationToken);

        if (inLedger)
            return Result.Failure(OtherIncomingFundErrors.CannotModifyVerified);

        dbContext.OtherIncomingFunds.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
