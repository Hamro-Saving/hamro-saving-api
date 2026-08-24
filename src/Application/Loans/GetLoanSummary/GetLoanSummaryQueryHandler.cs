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

        // A SuperAdmin may read across groups; everyone else is pinned to theirs.
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<LoanSummaryResponse>(groupResult.Error);
        var groupId = groupResult.Value;

        if (groupId.HasValue)
        {
            loansQuery = loansQuery.Where(l => l.GroupId == groupId.Value);
        }

        var loans = await loansQuery.ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var totalOutstandingPrincipal = loans.Sum(l => l.OutstandingPrincipal);
        var totalAccruedInterest = loans.Sum(l => l.InterestAccruedAsOf(now));

        return Result.Success(new LoanSummaryResponse(
            loans.Count,
            loans.Count(l => l.Status == LoanStatus.Active),
            loans.Count(l => l.Status == LoanStatus.PaidOff),
            loans.Count(l => l.Status == LoanStatus.Overdue),
            loans.Sum(l => l.Amount),
            totalOutstandingPrincipal,
            totalAccruedInterest,
            loans.Sum(l => l.TotalPrincipalPaid),
            loans.Sum(l => l.TotalInterestPaid),
            totalOutstandingPrincipal + totalAccruedInterest));
    }
}
