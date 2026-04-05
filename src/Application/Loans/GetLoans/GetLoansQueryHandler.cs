using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.GetLoans;

internal sealed class GetLoansQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetLoansQuery, List<LoanResponse>>
{
    public async Task<Result<List<LoanResponse>>> Handle(GetLoansQuery query, CancellationToken cancellationToken = default)
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

        if (query.BorrowerId.HasValue)
        {
            loansQuery = loansQuery.Where(l => l.BorrowerId == query.BorrowerId.Value);
        }

        if (query.Status.HasValue)
        {
            loansQuery = loansQuery.Where(l => l.Status == query.Status.Value);
        }

        var loans = await loansQuery
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        var loanIds = loans.Select(l => l.Id).ToList();

        var allBorrowerIds = loans.Select(l => l.BorrowerId).Distinct().ToList();

        var allBorrowers = await dbContext.Members
            .Where(m => allBorrowerIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName })
            .ToListAsync(cancellationToken);

        var approvalCounts = await dbContext.LoanApprovals
            .Where(a => loanIds.Contains(a.LoanId))
            .GroupBy(a => a.LoanId)
            .Select(g => new { LoanId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var allApprovals = await dbContext.LoanApprovals
            .Where(a => loanIds.Contains(a.LoanId))
            .Select(a => new { a.LoanId, a.ApproverId, a.ApprovedAt })
            .ToListAsync(cancellationToken);

        var approverIds = allApprovals.Select(a => a.ApproverId).Distinct().ToList();
        var approverUsers = await dbContext.Users
            .Where(u => approverIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                Name = dbContext.Members
                    .Where(m => m.Id == u.MemberId)
                    .Select(m => m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName)
                    .FirstOrDefault() ?? "Unknown"
            })
            .ToListAsync(cancellationToken);
        var approverDict = approverUsers.ToDictionary(u => u.Id, u => u.Name);

        var approvalsByLoan = allApprovals
            .GroupBy(a => a.LoanId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => new ApproverInfo(a.ApproverId, approverDict.GetValueOrDefault(a.ApproverId, "Unknown"), a.ApprovedAt)).ToList());

        var currentUserApprovals = allApprovals
            .Where(a => a.ApproverId == userContext.UserId)
            .Select(a => a.LoanId)
            .ToHashSet();

        var totalGroupMembers = await dbContext.Members
            .CountAsync(m => m.GroupId == userContext.GroupId && m.MembershipType == Domain.Members.MembershipType.Member, cancellationToken);

        var borrowerDict = allBorrowers.ToDictionary(m => m.Id, m => m.Name);
        var approvalCountDict = approvalCounts.ToDictionary(a => a.LoanId, a => a.Count);

        var now = DateTime.UtcNow;
        var response = loans.Select(l =>
        {
            var borrowerName = borrowerDict.GetValueOrDefault(l.BorrowerId, "Unknown");

            var approvalCount = approvalCountDict.GetValueOrDefault(l.Id, 0);
            var requiredApprovals = (int)Math.Ceiling(totalGroupMembers / 2.0);
            var elapsedDays = l.Status == LoanStatus.Active ? (now - l.StartDate).Days : 0;
            var accruedInterest = l.Amount * (l.InterestRate / 100m) * elapsedDays / 365m;

            return new LoanResponse(
                l.Id,
                l.BorrowerId,
                borrowerName,
                l.BorrowerType,
                l.GroupId,
                l.Amount,
                l.InterestRate,
                l.TotalInterest,
                l.TotalDue,
                Math.Round(accruedInterest, 2),
                l.StartDate,
                l.DueDate,
                l.Status,
                l.Notes,
                l.ApprovedById,
                approvalCount,
                requiredApprovals,
                currentUserApprovals.Contains(l.Id),
                approvalsByLoan.GetValueOrDefault(l.Id, []),
                l.CreatedAt);
        }).ToList();

        return Result.Success(response);
    }
}
