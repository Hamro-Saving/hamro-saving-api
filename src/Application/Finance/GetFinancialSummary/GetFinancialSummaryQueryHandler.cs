using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Loans;
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
        Guid? groupId = userContext.IsSuperAdmin ? query.GroupId : userContext.GroupId;

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

        var activeLoans = await loansQuery
            .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
            .ToListAsync(cancellationToken);

        // Money actually still out with borrowers, not what was originally lent
        var totalOnLoan = activeLoans.Sum(l => l.OutstandingPrincipal);

        if (groupId.HasValue)
        {
            var loanIds = await loansQuery.Select(l => l.Id).ToListAsync(cancellationToken);
            paymentsQuery = paymentsQuery.Where(p => loanIds.Contains(p.LoanId));
        }

        var loanInterestCollected = await paymentsQuery
            .Where(p => p.IsVerified)
            .SumAsync(p => (decimal?)p.InterestAmount, cancellationToken) ?? 0;

        // Interest a withdrawn fixed deposit actually paid out is income too, and without it
        // that money would disappear from the books the moment the deposit is closed.
        var fixedDepositInterest = await fixedDepositsQuery
            .Where(fd => fd.Status == FixedDepositStatus.Withdrawn)
            .SumAsync(fd => fd.InterestEarned, cancellationToken) ?? 0;

        var totalInterestCollected = loanInterestCollected + fixedDepositInterest;

        var totalExpenses = await expensesQuery
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;

        // Matured money is still sitting with the institution until it is withdrawn,
        // so it stays in the fixed-deposit bucket and out of in-hand cash.
        var totalFixedDeposits = await fixedDepositsQuery
            .Where(fd => fd.Status != FixedDepositStatus.Withdrawn)
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
