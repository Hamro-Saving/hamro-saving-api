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

        if (!userContext.IsSuperAdmin)
        {
            var groupId = userContext.GroupId;
            fdQuery = fdQuery.Where(fd => fd.GroupId == groupId);
        }
        else if (query.GroupId.HasValue)
        {
            fdQuery = fdQuery.Where(fd => fd.GroupId == query.GroupId.Value);
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
                fd.InterestEarned,
                fd.WithdrawnAt,
                fd.CreatedAt))
            .ToList();

        return Result.Success(response);
    }
}
