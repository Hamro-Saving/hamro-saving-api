using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.GetLoanSummary;

internal sealed class GetLoanSummaryQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetLoanSummaryQuery, LoanSummaryResponse>
{
    public async Task<Result<LoanSummaryResponse>> Handle(GetLoanSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var loansQuery = dbContext.Loans.AsQueryable();

        if (!userContext.IsSuperAdmin)
        {
            var groupId = userContext.GroupId;
            loansQuery = loansQuery.Where(l => l.GroupId == groupId);
        }
        else if (query.GroupId.HasValue)
        {
            loansQuery = loansQuery.Where(l => l.GroupId == query.GroupId.Value);
        }

        var loans = await loansQuery.ToListAsync(cancellationToken);

        var loanIds = loans.Select(l => l.Id).ToList();
        var totalPaid = await dbContext.LoanPayments
            .Where(p => loanIds.Contains(p.LoanId))
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;

        var totalPrincipal = loans.Sum(l => l.Amount);
        var totalInterest = loans.Sum(l => l.TotalInterest);
        var totalDue = loans.Sum(l => l.TotalDue);

        return Result.Success(new LoanSummaryResponse(
            loans.Count,
            loans.Count(l => l.Status == LoanStatus.Active),
            loans.Count(l => l.Status == LoanStatus.PaidOff),
            loans.Count(l => l.Status == LoanStatus.Overdue),
            totalPrincipal,
            totalInterest,
            totalDue,
            totalPaid,
            totalDue - totalPaid));
    }
}
