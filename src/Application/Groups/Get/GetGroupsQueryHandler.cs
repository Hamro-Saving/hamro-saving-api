using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Groups.Get;

internal sealed class GetGroupsQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetGroupsQuery, List<GroupResponse>>
{
    public async Task<Result<List<GroupResponse>>> Handle(GetGroupsQuery query, CancellationToken cancellationToken = default)
    {
        var groupsQuery = dbContext.Groups.AsQueryable();

        if (!userContext.IsSuperAdmin)
        {
            groupsQuery = groupsQuery.Where(g => g.Id == userContext.ActiveGroupId);
        }

        var groups = await groupsQuery
            .OrderBy(g => g.Name)
            .Select(g => new GroupResponse(
                g.Id,
                g.Name,
                g.Code,
                g.Description,
                g.IsActive,
                g.MemberInterestRate,
                g.NonMemberInterestRate,
                g.ValidFrom,
                g.ValidTo,
                g.CreatedAt,
                g.UpdatedAt,
                dbContext.Members.Count(m => m.GroupId == g.Id && m.IsActive &&
                    m.GroupRole != GroupRole.NonMember)))
            .ToListAsync(cancellationToken);

        return Result.Success(groups);
    }
}
