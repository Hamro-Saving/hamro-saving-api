using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.GetOtherIncomingFunds;

internal sealed class GetOtherIncomingFundsQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetOtherIncomingFundsQuery, List<OtherIncomingFundResponse>>
{
    public async Task<Result<List<OtherIncomingFundResponse>>> Handle(GetOtherIncomingFundsQuery query, CancellationToken cancellationToken = default)
    {
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<List<OtherIncomingFundResponse>>(groupResult.Error);
        var groupId = groupResult.Value;

        var records = dbContext.OtherIncomingFunds.AsQueryable();

        if (groupId.HasValue)
            records = records.Where(r => r.GroupId == groupId.Value);

        var rows = await records
            .OrderByDescending(r => r.PaidDate)
            .Select(r => new OtherIncomingFundResponse(
                r.Id,
                r.MemberId,
                dbContext.Members
                    .Where(m => m.Id == r.MemberId)
                    .Select(m => m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName)
                    .FirstOrDefault() ?? "Unknown",
                r.Amount,
                r.PaidDate,
                r.Remarks,
                r.IsVerified,
                r.VerifiedAt,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(rows);
    }
}
