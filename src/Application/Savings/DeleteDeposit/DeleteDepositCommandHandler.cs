using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Savings;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Savings.DeleteDeposit;

/// <summary>
/// Removes a deposit that was recorded in error. Only while it is unverified: once
/// verified the money is in the group's books, and a ledger entry is never unwritten —
/// correcting that is an opposite entry, not a deletion.
/// </summary>
internal sealed class DeleteDepositCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<DeleteDepositCommand>
{
    public async Task<Result> Handle(DeleteDepositCommand command, CancellationToken cancellationToken = default)
    {
        var deposit = await dbContext.Deposits
            .FirstOrDefaultAsync(d => d.Id == command.DepositId, cancellationToken);

        if (deposit is null)
            return Result.Failure(DepositErrors.NotFound(command.DepositId));

        if (!userContext.CanWrite(deposit.GroupId))
            return Result.Failure(DepositErrors.NotInGroup);

        // The person it was recorded against may remove their own; an admin any of them.
        if (!userContext.IsGroupAdmin && deposit.MemberId != userContext.ActiveMemberId)
            return Result.Failure(UserErrors.Unauthorized);

        if (deposit.IsVerified)
            return Result.Failure(DepositErrors.CannotDeleteVerified);

        // Belt and braces: an unverified deposit should have no entries, and if one
        // somehow exists then deleting the record would orphan the books.
        var inLedger = await dbContext.LedgerEntries
            .AnyAsync(e => e.SourceType == "Deposit" && e.SourceId == deposit.Id, cancellationToken);

        if (inLedger)
            return Result.Failure(DepositErrors.CannotDeleteVerified);

        dbContext.Deposits.Remove(deposit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
