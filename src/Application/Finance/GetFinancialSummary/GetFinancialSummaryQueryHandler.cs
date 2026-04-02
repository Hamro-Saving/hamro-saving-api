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

        var totalOnLoan = activeLoans.Sum(l => l.Amount);

        if (groupId.HasValue)
        {
            var loanIds = await loansQuery.Select(l => l.Id).ToListAsync(cancellationToken);
            paymentsQuery = paymentsQuery.Where(p => loanIds.Contains(p.LoanId));
        }

        var totalInterestCollected = await paymentsQuery
            .Where(p => p.IsVerified)
            .SumAsync(p => (decimal?)p.InterestAmount, cancellationToken) ?? 0;

        var totalExpenses = await expensesQuery
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;

        var totalFixedDeposits = await fixedDepositsQuery
            .Where(fd => fd.Status == FixedDepositStatus.Active)
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
