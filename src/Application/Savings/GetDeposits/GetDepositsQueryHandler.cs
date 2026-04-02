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

        if (!userContext.IsSuperAdmin)
        {
            var groupId = userContext.GroupId;
            depositsQuery = depositsQuery.Where(d => d.GroupId == groupId);
        }
        else if (query.GroupId.HasValue)
        {
            depositsQuery = depositsQuery.Where(d => d.GroupId == query.GroupId.Value);
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
            .Join(dbContext.Users,
                d => d.MemberId,
                u => u.Id,
                (d, u) => new DepositResponse(
                    d.Id,
                    d.MemberId,
                    u.FirstName + " " + u.LastName,
                    d.GroupId,
                    d.Amount,
                    d.DepositMonth,
                    d.DepositYear,
                    d.Type,
                    d.Notes,
                    d.IsVerified,
                    d.VerifiedById,
                    d.VerifiedAt,
                    d.CreatedAt))
            .OrderByDescending(d => d.DepositYear)
            .ThenByDescending(d => d.DepositMonth)
            .ThenBy(d => d.MemberName)
            .ToListAsync(cancellationToken);

        return Result.Success(deposits);
    }
}
