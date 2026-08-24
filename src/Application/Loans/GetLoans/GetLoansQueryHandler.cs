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

        // A SuperAdmin may read across groups; everyone else is pinned to theirs.
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<List<LoanResponse>>(groupResult.Error);
        var groupId = groupResult.Value;

        if (groupId.HasValue)
        {
            loansQuery = loansQuery.Where(l => l.GroupId == groupId.Value);
        }

        if (userContext.SeesOnlyOwnRecords())
        {
            var ownMemberId = userContext.ActiveMemberId;
            loansQuery = loansQuery.Where(l => l.BorrowerId == ownMemberId);
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

        var allVotes = await dbContext.LoanApprovals
            .Where(a => loanIds.Contains(a.LoanId))
            .Select(a => new { a.LoanId, a.ApproverId, a.IsApproved, a.ApprovedAt })
            .ToListAsync(cancellationToken);

        var voterIds = allVotes.Select(a => a.ApproverId).Distinct().ToList();
        var voterUsers = await dbContext.Users
            .Where(u => voterIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                Name = dbContext.Members
                    .Where(m => m.UserId == u.Id)
                    .Select(m => m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName)
                    .FirstOrDefault() ?? "Unknown"
            })
            .ToListAsync(cancellationToken);
        var voterDict = voterUsers.ToDictionary(u => u.Id, u => u.Name);

        var votesByLoan = allVotes
            .GroupBy(a => a.LoanId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Every loan needs a majority of its own group's voters, so count per group.
        var groupIds = loans.Select(l => l.GroupId).Distinct().ToList();
        var voterCountByGroup = (await LoanVoting.EligibleVoters(dbContext)
            .Where(m => groupIds.Contains(m.GroupId))
            .GroupBy(m => m.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.GroupId, x => x.Count);

        var borrowerDict = allBorrowers.ToDictionary(m => m.Id, m => m.Name);

        var now = DateTime.UtcNow;
        var response = loans.Select(l =>
        {
            var borrowerName = borrowerDict.GetValueOrDefault(l.BorrowerId, "Unknown");

            var votes = votesByLoan.GetValueOrDefault(l.Id, []);
            var approvers = votes
                .Where(v => v.IsApproved)
                .Select(v => new ApproverInfo(v.ApproverId, voterDict.GetValueOrDefault(v.ApproverId, "Unknown"), v.ApprovedAt))
                .ToList();
            var decliners = votes
                .Where(v => !v.IsApproved)
                .Select(v => new ApproverInfo(v.ApproverId, voterDict.GetValueOrDefault(v.ApproverId, "Unknown"), v.ApprovedAt))
                .ToList();

            var requiredApprovals = LoanVoting.VotesNeeded(voterCountByGroup.GetValueOrDefault(l.GroupId, 0));

            return new LoanResponse(
                l.Id,
                l.BorrowerId,
                borrowerName,
                l.BorrowerType,
                l.GroupId,
                l.Amount,
                l.InterestRate,
                l.OutstandingPrincipal,
                l.InterestAccruedAsOf(now),
                l.PayoffAmountAsOf(now),
                l.DailyInterest,
                l.UnpaidInterest,
                l.TotalPrincipalPaid,
                l.TotalInterestPaid,
                l.DisbursedAt,
                l.LastAccrualDate,
                l.StartDate,
                l.DueDate,
                l.Status,
                l.Notes,
                l.DisbursedById,
                approvers.Count,
                decliners.Count,
                requiredApprovals,
                approvers.Any(a => a.ApproverId == userContext.UserId),
                decliners.Any(a => a.ApproverId == userContext.UserId),
                approvers,
                decliners,
                l.CreatedAt);
        }).ToList();

        if (userContext.SeesOnlyOwnRecords())
        {
            response = response.Select(r => r.WithoutGroupInternals()).ToList();
        }

        return Result.Success(response);
    }
}
