using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Groups.Get;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Groups.GetById;

internal sealed class GetGroupByIdQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetGroupByIdQuery, GroupResponse>
{
    public async Task<Result<GroupResponse>> Handle(GetGroupByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSuperAdmin && userContext.GroupId != query.GroupId)
        {
            return Result.Failure<GroupResponse>(GroupErrors.NotFound(query.GroupId));
        }

        var group = await dbContext.Groups
            .Where(g => g.Id == query.GroupId)
            .Select(g => new GroupResponse(
                g.Id,
                g.Name,
                g.Code,
                g.Description,
                g.IsActive,
                g.MemberInterestRate,
                g.NonMemberInterestRate,
                g.CreatedAt,
                g.UpdatedAt,
                dbContext.Members.Count(m => m.GroupId == g.Id && m.IsActive &&
                    m.MembershipType == MembershipType.Member)))
            .FirstOrDefaultAsync(cancellationToken);

        if (group is null)
        {
            return Result.Failure<GroupResponse>(GroupErrors.NotFound(query.GroupId));
        }

        return Result.Success(group);
    }
}
