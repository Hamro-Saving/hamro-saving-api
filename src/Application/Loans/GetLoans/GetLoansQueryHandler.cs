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

        var memberDict = members.ToDictionary(m => m.Id, m => m.Name);
        var nonMemberDict = nonMembers.ToDictionary(nm => nm.Id, nm => nm.Name);

        var response = loans.Select(l =>
        {
            var borrowerName = l.BorrowerType == "Member"
                ? memberDict.GetValueOrDefault(l.BorrowerId, "Unknown")
                : nonMemberDict.GetValueOrDefault(l.BorrowerId, "Unknown");

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
                l.StartDate,
                l.DueDate,
                l.Status,
                l.Notes,
                l.ApprovedById,
                l.CreatedAt);
        }).ToList();

        return Result.Success(response);
    }
}
