using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Ledger;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.ForceDisburseLoan;

/// <summary>
/// Pays out a loan the members never voted on. The tally is counted here rather than taken
/// from the loan's status, because the whole point is that the vote never settled — and the
/// denominator is the same <see cref="LoanVoting.EligibleVoters"/> set the vote itself uses,
/// so the two can't disagree about what half the group means.
/// </summary>
internal sealed class ForceDisburseLoanCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<ForceDisburseLoanCommand>
{
    public async Task<Result> Handle(ForceDisburseLoanCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.CanWrite(loan.GroupId))
            return Result.Failure(LoanErrors.NotInGroup);

        var totalVoters = await LoanVoting.EligibleVoters(dbContext)
            .CountAsync(m => m.GroupId == loan.GroupId, cancellationToken);

        var declines = await dbContext.LoanApprovals
            .CountAsync(a => a.LoanId == loan.Id && !a.IsApproved, cancellationToken);

        var votes = new LoanVoteTally(declines, LoanVoting.DeclinesNeeded(totalVoters));
        var inHand = await CashPosition.InHandAsync(dbContext, loan.GroupId, cancellationToken);

        // A date with no time of day: the payout is recorded as of midnight UTC on that day,
        // which is what the interest clock counts in.
        var disbursedAt = command.DisbursedOn is { } on
            ? on.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var result = loan.ForceDisbursement(userContext.UserId, disbursedAt, inHand, votes);
        if (result.IsFailure) return result;

        dbContext.PostLoanDisbursement(loan.GroupId, loan.Id, loan.BorrowerId, loan.Amount,
            loan.DisbursedAt ?? DateTime.UtcNow, "Loan force disbursed");

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
