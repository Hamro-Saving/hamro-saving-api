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

        var memberBorrowerIds = loans.Where(l => l.BorrowerType == "Member").Select(l => l.BorrowerId).Distinct().ToList();
        var nonMemberBorrowerIds = loans.Where(l => l.BorrowerType == "NonMember").Select(l => l.BorrowerId).Distinct().ToList();

        var members = await dbContext.Users
            .Where(u => memberBorrowerIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(cancellationToken);

        var nonMembers = await dbContext.NonMembers
            .Where(nm => nonMemberBorrowerIds.Contains(nm.Id))
            .Select(nm => new { nm.Id, Name = nm.FullName })
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
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
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

        var totalGroupMembers = await dbContext.Users
            .CountAsync(u => u.GroupId == userContext.GroupId && u.Role == Domain.Users.UserRole.Member, cancellationToken);

        var memberDict = members.ToDictionary(m => m.Id, m => m.Name);
        var nonMemberDict = nonMembers.ToDictionary(nm => nm.Id, nm => nm.Name);
        var approvalCountDict = approvalCounts.ToDictionary(a => a.LoanId, a => a.Count);

        var now = DateTime.UtcNow;
        var response = loans.Select(l =>
        {
            var borrowerName = l.BorrowerType == "Member"
                ? memberDict.GetValueOrDefault(l.BorrowerId, "Unknown")
                : nonMemberDict.GetValueOrDefault(l.BorrowerId, "Unknown");

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
