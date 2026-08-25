using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Savings.GetDeposits;

internal sealed class GetDepositsQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetDepositsQuery, List<DepositResponse>>
{
    public async Task<Result<List<DepositResponse>>> Handle(GetDepositsQuery query, CancellationToken cancellationToken = default)
    {
        var depositsQuery = dbContext.Deposits.AsQueryable();

        // A SuperAdmin may read across groups; everyone else is pinned to theirs.
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<List<DepositResponse>>(groupResult.Error);
        var groupId = groupResult.Value;

        if (groupId.HasValue)
        {
            depositsQuery = depositsQuery.Where(d => d.GroupId == groupId.Value);
        }

        if (query.MemberId.HasValue)
        {
            depositsQuery = depositsQuery.Where(d => d.MemberId == query.MemberId.Value);
        }

        if (query.Month.HasValue)
        {
            depositsQuery = depositsQuery.Where(d => d.DepositMonth == query.Month.Value);
        }

        if (query.Year.HasValue)
        {
            depositsQuery = depositsQuery.Where(d => d.DepositYear == query.Year.Value);
        }

        if (query.IsVerified.HasValue)
        {
            depositsQuery = depositsQuery.Where(d => d.IsVerified == query.IsVerified.Value);
        }

        var deposits = await depositsQuery
            .Join(dbContext.Members,
                d => d.MemberId,
                m => m.Id,
                (d, m) => new
                {
                    d.Id,
                    d.MemberId,
                    MemberName = m.FirstName + " " + m.LastName,
                    d.GroupId,
                    d.Amount,
                    d.DepositMonth,
                    d.DepositYear,
                    d.DepositDate,
                    d.Type,
                    d.Notes,
                    d.IsVerified,
                    d.VerifiedById,
                    d.VerifiedAt,
                    d.CreatedAt
                })
            // Newest first, by the day the money changed hands. Sorting by the BS period
            // instead stranded every "Other" deposit, which has no period at all.
            .OrderByDescending(d => d.DepositDate)
            .ThenBy(d => d.MemberName)
            .Select(d => new DepositResponse(
                d.Id,
                d.MemberId,
                d.MemberName,
                d.GroupId,
                d.Amount,
                d.DepositMonth,
                d.DepositYear,
                d.DepositDate,
                d.Type,
                d.Notes,
                d.IsVerified,
                d.VerifiedById,
                d.VerifiedAt,
                d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(deposits);
    }
}
