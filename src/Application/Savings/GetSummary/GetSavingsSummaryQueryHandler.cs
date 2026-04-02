using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Savings;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Savings.GetSummary;

internal sealed class GetSavingsSummaryQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetSavingsSummaryQuery, SavingsSummaryResponse>
{
    public async Task<Result<SavingsSummaryResponse>> Handle(GetSavingsSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var depositsQuery = dbContext.Deposits.AsQueryable();

        if (!userContext.IsSuperAdmin)
        {
            var groupId = userContext.GroupId;
            depositsQuery = depositsQuery.Where(d => d.GroupId == groupId);
        }
        else if (query.GroupId.HasValue)
        {
            depositsQuery = depositsQuery.Where(d => d.GroupId == query.GroupId.Value);
        }

        var deposits = await depositsQuery.ToListAsync(cancellationToken);

        var memberIds = deposits.Select(d => d.MemberId).Distinct().ToList();
        var members = await dbContext.Users
            .Where(u => memberIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(cancellationToken);

        var memberDict = members.ToDictionary(m => m.Id, m => m.Name);

        var totalDeposits = deposits.Sum(d => d.Amount);
        var totalVerified = deposits.Where(d => d.IsVerified).Sum(d => d.Amount);
        var totalPending = deposits.Where(d => !d.IsVerified).Sum(d => d.Amount);

        var byType = deposits
            .GroupBy(d => d.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Amount));

        var byMember = deposits
            .GroupBy(d => d.MemberId)
            .Select(g => new MemberDepositSummary(
                g.Key,
                memberDict.GetValueOrDefault(g.Key, "Unknown"),
                g.Sum(d => d.Amount),
                g.Count()))
            .OrderByDescending(m => m.TotalAmount)
            .ToList();

        return Result.Success(new SavingsSummaryResponse(
            totalDeposits,
            totalVerified,
            totalPending,
            byType,
            byMember));
    }
}
