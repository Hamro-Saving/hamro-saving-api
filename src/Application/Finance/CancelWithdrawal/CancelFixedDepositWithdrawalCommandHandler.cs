using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Ledger;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.CancelWithdrawal;

/// <summary>
/// Takes back a withdrawal recorded in error, leaving the deposit placed as it was. Only
/// while unverified: a verified withdrawal is money that has already come back in the books.
/// </summary>
internal sealed class CancelFixedDepositWithdrawalCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CancelFixedDepositWithdrawalCommand>
{
    public async Task<Result> Handle(CancelFixedDepositWithdrawalCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var fixedDeposit = await dbContext.FixedDeposits
            .FirstOrDefaultAsync(fd => fd.Id == command.FixedDepositId, cancellationToken);

        if (fixedDeposit is null)
            return Result.Failure(FixedDepositErrors.NotFound(command.FixedDepositId));

        if (!userContext.CanWrite(fixedDeposit.GroupId))
            return Result.Failure(FixedDepositErrors.NotInGroup);

        // Belt and braces. The placement and the withdrawal share a source, so this asks
        // specifically whether the return has been posted rather than whether anything has.
        var inLedger = await dbContext.LedgerEntries
            .AnyAsync(e => e.SourceType == "FixedDeposit"
                        && e.SourceId == fixedDeposit.Id
                        && (e.Type == TransactionType.FixedDepositWithdrawal
                         || e.Type == TransactionType.FixedDepositInterest),
                cancellationToken);

        if (inLedger)
            return Result.Failure(FixedDepositErrors.WithdrawalAlreadyVerified);

        var result = fixedDeposit.CancelWithdrawal();
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
