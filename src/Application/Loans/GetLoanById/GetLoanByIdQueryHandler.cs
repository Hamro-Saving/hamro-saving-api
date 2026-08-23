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

        var borrower = await dbContext.Members
            .Where(m => m.Id == loan.BorrowerId)
            .Select(m => new { Name = m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName })
            .FirstOrDefaultAsync(cancellationToken);
        var borrowerName = borrower?.Name ?? "Unknown";

        var votes = await dbContext.LoanApprovals
            .Where(a => a.LoanId == loan.Id)
            .Select(a => new { a.ApproverId, a.IsApproved, a.ApprovedAt })
            .ToListAsync(cancellationToken);

        var voterIds = votes.Select(a => a.ApproverId).ToList();
        var voterNames = await dbContext.Users
            .Where(u => voterIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                Name = dbContext.Members
                    .Where(m => m.Id == u.MemberId)
                    .Select(m => m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName)
                    .FirstOrDefault() ?? "Unknown"
            })
            .ToListAsync(cancellationToken);
        var voterDict = voterNames.ToDictionary(u => u.Id, u => u.Name);

        var approverList = votes
            .Where(v => v.IsApproved)
            .Select(v => new ApproverInfo(v.ApproverId, voterDict.GetValueOrDefault(v.ApproverId, "Unknown"), v.ApprovedAt))
            .ToList();
        var declinerList = votes
            .Where(v => !v.IsApproved)
            .Select(v => new ApproverInfo(v.ApproverId, voterDict.GetValueOrDefault(v.ApproverId, "Unknown"), v.ApprovedAt))
            .ToList();

        var totalVoters = await LoanVoting.EligibleVoters(dbContext)
            .CountAsync(m => m.GroupId == loan.GroupId, cancellationToken);

        var requiredApprovals = LoanVoting.VotesNeeded(totalVoters);
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
            loan.DisbursedById,
            approverList.Count,
            declinerList.Count,
            requiredApprovals,
            approverList.Any(a => a.ApproverId == userContext.UserId),
            declinerList.Any(a => a.ApproverId == userContext.UserId),
            approverList,
            declinerList,
            loan.CreatedAt));
    }
}
