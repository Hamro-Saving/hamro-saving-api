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
            .Select(fd => new FixedDepositResponse(
                fd.Id,
                fd.GroupId,
                fd.InstitutionName,
                fd.Amount,
                fd.InterestRate,
                fd.ExpectedMaturityAmount,
                fd.StartDate,
                fd.MaturityDate,
                fd.Status,
                fd.Notes,
                fd.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(fixedDeposits);
    }
}
