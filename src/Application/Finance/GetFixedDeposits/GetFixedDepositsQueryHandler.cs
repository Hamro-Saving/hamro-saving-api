using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.GetFixedDeposits;

internal sealed class GetFixedDepositsQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetFixedDepositsQuery, List<FixedDepositResponse>>
{
    public async Task<Result<List<FixedDepositResponse>>> Handle(GetFixedDepositsQuery query, CancellationToken cancellationToken = default)
    {
        var fdQuery = dbContext.FixedDeposits.AsQueryable();

        // A SuperAdmin may read across groups; everyone else is pinned to theirs.
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<List<FixedDepositResponse>>(groupResult.Error);
        var groupId = groupResult.Value;

        if (groupId.HasValue)
        {
            fdQuery = fdQuery.Where(fd => fd.GroupId == groupId.Value);
        }

        var fixedDeposits = await fdQuery
            .OrderByDescending(fd => fd.StartDate)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var response = fixedDeposits
            .Select(fd => new FixedDepositResponse(
                fd.Id,
                fd.GroupId,
                fd.InstitutionName,
                fd.Amount,
                fd.InterestRate,
                fd.ExpectedMaturityAmount,
                fd.StartDate,
                fd.MaturityDate,
                // Reported as matured from the maturity date on, without waiting for anyone to record it
                fd.StatusAsOf(now),
                fd.Notes,
                fd.IsVerified,
                fd.VerifiedAt,
                fd.InterestEarned,
                fd.WithdrawnAt,
                fd.IsWithdrawalVerified,
                fd.WithdrawalVerifiedAt,
                fd.CreatedAt))
            .ToList();

        return Result.Success(response);
    }
}
