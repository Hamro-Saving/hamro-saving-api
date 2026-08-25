using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.GetLateJoinerInterest;

internal sealed class GetLateJoinerInterestQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetLateJoinerInterestQuery, List<LateJoinerInterestResponse>>
{
    public async Task<Result<List<LateJoinerInterestResponse>>> Handle(GetLateJoinerInterestQuery query, CancellationToken cancellationToken = default)
    {
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<List<LateJoinerInterestResponse>>(groupResult.Error);
        var groupId = groupResult.Value;

        var records = dbContext.LateJoinerInterests.AsQueryable();

        if (groupId.HasValue)
            records = records.Where(r => r.GroupId == groupId.Value);

        var rows = await records
            .OrderByDescending(r => r.PaidDate)
            .Select(r => new LateJoinerInterestResponse(
                r.Id,
                r.MemberId,
                dbContext.Members
                    .Where(m => m.Id == r.MemberId)
                    .Select(m => m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName)
                    .FirstOrDefault() ?? "Unknown",
                r.Amount,
                r.PaidDate,
                r.Notes,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(rows);
    }
}
