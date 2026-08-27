using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Savings;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.GetFinancialSummary;

internal sealed class GetFinancialSummaryQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetFinancialSummaryQuery, FinancialSummaryResponse>
{
    public async Task<Result<FinancialSummaryResponse>> Handle(GetFinancialSummaryQuery query, CancellationToken cancellationToken = default)
    {
        // A SuperAdmin may read across groups; everyone else is pinned to theirs.
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<FinancialSummaryResponse>(groupResult.Error);
        Guid? groupId = groupResult.Value;

        var depositsQuery = dbContext.Deposits.AsQueryable();
        var loansQuery = dbContext.Loans.AsQueryable();
        var paymentsQuery = dbContext.LoanPayments.AsQueryable();
        var expensesQuery = dbContext.Expenses.AsQueryable();
        var fixedDepositsQuery = dbContext.FixedDeposits.AsQueryable();

        if (groupId.HasValue)
        {
            depositsQuery = depositsQuery.Where(d => d.GroupId == groupId.Value);
            loansQuery = loansQuery.Where(l => l.GroupId == groupId.Value);
            expensesQuery = expensesQuery.Where(e => e.GroupId == groupId.Value);
            fixedDepositsQuery = fixedDepositsQuery.Where(fd => fd.GroupId == groupId.Value);
        }

        var totalSavings = await depositsQuery
            .Where(d => d.IsVerified)
            .SumAsync(d => (decimal?)d.Amount, cancellationToken) ?? 0;

        if (groupId.HasValue)
        {
            var loanIds = await loansQuery.Select(l => l.Id).ToListAsync(cancellationToken);
            paymentsQuery = paymentsQuery.Where(p => loanIds.Contains(p.LoanId));
        }

        // Everything that has left the group for a borrower. A loan that was cancelled or
        // declined never went out, so only a disbursed one counts.
        var principalDisbursed = await loansQuery
            .Where(l => l.DisbursedAt != null)
            .SumAsync(l => (decimal?)l.Amount, cancellationToken) ?? 0;

        var principalRepaid = await paymentsQuery
            .Where(p => p.IsVerified)
            .SumAsync(p => (decimal?)p.PrincipalAmount, cancellationToken) ?? 0;

        // Money still out with borrowers. Deliberately not the loans' OutstandingPrincipal:
        // that falls the moment a repayment is keyed in, while the ledger only credits the
        // receivable once the payment is verified. Reading it here made the two disagree by
        // the value of every unverified repayment — and a repayment that settles a loan made
        // the whole of its principal vanish from this figure while still sitting in the
        // ledger as money out. Counting what was disbursed less what has been verified back
        // is the same question the ledger answers, so the two cannot drift.
        var totalOnLoan = principalDisbursed - principalRepaid;

        var loanInterestCollected = await paymentsQuery
            .Where(p => p.IsVerified)
            .SumAsync(p => (decimal?)p.InterestAmount, cancellationToken) ?? 0;

        // Counted from the verified withdrawal, not the withdrawn status: the interest becomes
        // income when it is posted, and recording a withdrawal no longer posts it.
        var fixedDepositInterest = await fixedDepositsQuery
            .Where(fd => fd.IsWithdrawalVerified)
            .SumAsync(fd => fd.InterestEarned, cancellationToken) ?? 0;

        var lateJoinerQuery = dbContext.OtherIncomingFunds.AsQueryable();
        if (groupId.HasValue)
            lateJoinerQuery = lateJoinerQuery.Where(r => r.GroupId == groupId.Value);

        var lateJoinerInterest = await lateJoinerQuery
            .Where(r => r.IsVerified)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0;

        var totalInterestCollected = loanInterestCollected + fixedDepositInterest + lateJoinerInterest;

        // Counting an unverified one would spend the money on this page while the spending
        // limit, which reads the ledger, still says it is available.
        var totalExpenses = await expensesQuery
            .Where(e => e.IsVerified)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;

        // What the ledger holds as placed: verified on and not yet verified back off. Money
        // recorded as withdrawn but unchecked stays here — the ledger has not been told it
        // came back.
        var totalFixedDeposits = await fixedDepositsQuery
            .Where(fd => fd.IsVerified && !fd.IsWithdrawalVerified)
            .SumAsync(fd => (decimal?)fd.Amount, cancellationToken) ?? 0;

        var inHandCash = totalSavings + totalInterestCollected - totalOnLoan - totalExpenses - totalFixedDeposits;

        return Result.Success(new FinancialSummaryResponse(
            totalSavings,
            totalOnLoan,
            totalInterestCollected,
            totalExpenses,
            totalFixedDeposits,
            inHandCash));
    }
}
