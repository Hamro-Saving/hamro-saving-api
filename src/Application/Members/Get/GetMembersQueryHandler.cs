using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.Get;

internal sealed class GetMembersQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetMembersQuery, List<MemberResponse>>
{
    public async Task<Result<List<MemberResponse>>> Handle(GetMembersQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Member> membersQuery = dbContext.Members;

        if (query.Roles is { Count: > 0 })
        {
            var roles = query.Roles;
            membersQuery = membersQuery.Where(m => roles.Contains(m.GroupRole));
        }

        // A SuperAdmin may read across groups; everyone else is pinned to theirs.
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<List<MemberResponse>>(groupResult.Error);
        var groupId = groupResult.Value;

        if (groupId.HasValue)
        {
            membersQuery = membersQuery.Where(m => m.GroupId == groupId.Value);
        }

        var members = await membersQuery
            .OrderBy(m => m.FirstName)
            .ThenBy(m => m.LastName)
            .Select(m => new MemberResponse(
                m.Id,
                m.Email,
                m.FirstName,
                m.LastName,
                m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName,
                m.GroupRole,
                m.GroupId,
                m.IsActive,
                dbContext.Users.Any(u => u.Id == m.UserId && u.IsActive),
                dbContext.Deposits
                    .Where(d => d.MemberId == m.Id && d.IsVerified)
                    .Sum(d => (decimal?)d.Amount) ?? 0,
                0m,
                0m,
                m.PhoneNumber,
                m.Address,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        var owed = await LoansOwedBy(dbContext, members.Select(m => m.Id).ToList(), cancellationToken);

        return Result.Success(members
            .Select(m => owed.TryGetValue(m.Id, out var o)
                ? m with { OutstandingPrincipal = o.Principal, OutstandingInterest = o.Interest }
                : m)
            .ToList());
    }

    /// <summary>What each borrower still owes: principal out, plus interest run to today.</summary>
    internal static async Task<Dictionary<Guid, (decimal Principal, decimal Interest)>> LoansOwedBy(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<Guid> borrowerIds,
        CancellationToken cancellationToken)
    {
        if (borrowerIds.Count == 0) return [];

        var loans = await dbContext.Loans
            .Where(l => borrowerIds.Contains(l.BorrowerId)
                     && (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        return loans
            .GroupBy(l => l.BorrowerId)
            .ToDictionary(
                g => g.Key,
                g => (g.Sum(l => l.OutstandingPrincipal), g.Sum(l => l.InterestAccruedAsOf(now))));
    }
}
