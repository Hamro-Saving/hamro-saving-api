using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.GetLoans;
using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.GetLoanById;

internal sealed class GetLoanByIdQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetLoanByIdQuery, LoanResponse>
{
    public async Task<Result<LoanResponse>> Handle(GetLoanByIdQuery query, CancellationToken cancellationToken = default)
    {
        var loanQuery = dbContext.Loans.Where(l => l.Id == query.LoanId);

        if (!userContext.IsSuperAdmin && userContext.GroupId.HasValue)
        {
            var groupId = userContext.GroupId;
            loanQuery = loanQuery.Where(l => l.GroupId == groupId);
        }

        var loan = await loanQuery.FirstOrDefaultAsync(cancellationToken);

        if (loan is null)
        {
            return Result.Failure<LoanResponse>(LoanErrors.NotFound(query.LoanId));
        }

        string borrowerName;
        if (loan.BorrowerType == "Member")
        {
            var member = await dbContext.Users
                .Where(u => u.Id == loan.BorrowerId)
                .Select(u => new { Name = u.FirstName + " " + u.LastName })
                .FirstOrDefaultAsync(cancellationToken);
            borrowerName = member?.Name ?? "Unknown";
        }
        else
        {
            var nonMember = await dbContext.NonMembers
                .Where(nm => nm.Id == loan.BorrowerId)
                .Select(nm => new { nm.FullName })
                .FirstOrDefaultAsync(cancellationToken);
            borrowerName = nonMember?.FullName ?? "Unknown";
        }

        var approvalCount = await dbContext.LoanApprovals
            .CountAsync(a => a.LoanId == loan.Id, cancellationToken);

        var hasCurrentUserApproved = await dbContext.LoanApprovals
            .AnyAsync(a => a.LoanId == loan.Id && a.ApproverId == userContext.UserId, cancellationToken);

        var approvals = await dbContext.LoanApprovals
            .Where(a => a.LoanId == loan.Id)
            .Select(a => new { a.ApproverId, a.ApprovedAt })
            .ToListAsync(cancellationToken);

        var approverIds = approvals.Select(a => a.ApproverId).ToList();
        var approverNames = await dbContext.Users
            .Where(u => approverIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(cancellationToken);
        var approverDict = approverNames.ToDictionary(u => u.Id, u => u.Name);

        var approverList = approvals
            .Select(a => new ApproverInfo(a.ApproverId, approverDict.GetValueOrDefault(a.ApproverId, "Unknown"), a.ApprovedAt))
            .ToList();

        var totalGroupMembers = await dbContext.Users
            .CountAsync(u => u.GroupId == loan.GroupId && u.Role == Domain.Users.UserRole.Member, cancellationToken);

        var requiredApprovals = (int)Math.Ceiling(totalGroupMembers / 2.0);
        var elapsedDays = loan.Status == LoanStatus.Active ? (DateTime.UtcNow - loan.StartDate).Days : 0;
        var accruedInterest = Math.Round(loan.Amount * (loan.InterestRate / 100m) * elapsedDays / 365m, 2);

        return Result.Success(new LoanResponse(
            loan.Id,
            loan.BorrowerId,
            borrowerName,
            loan.BorrowerType,
            loan.GroupId,
            loan.Amount,
            loan.InterestRate,
            loan.TotalInterest,
            loan.TotalDue,
            accruedInterest,
            loan.StartDate,
            loan.DueDate,
            loan.Status,
            loan.Notes,
            loan.ApprovedById,
            approvalCount,
            requiredApprovals,
            hasCurrentUserApproved,
            approverList,
            loan.CreatedAt));
    }
}
