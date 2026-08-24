using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.Get;

internal sealed class GetMembersQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetMembersQuery, List<MemberResponse>>
{
    public async Task<Result<List<MemberResponse>>> Handle(GetMembersQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Member> membersQuery = dbContext.Members;

        if (query.Roles is { Count: > 0 })
        {
            var roles = query.Roles;
            membersQuery = membersQuery.Where(m => roles.Contains(m.GroupRole));
        }

        // A SuperAdmin may read across groups; everyone else is pinned to theirs.
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<List<MemberResponse>>(groupResult.Error);
        var groupId = groupResult.Value;

        if (groupId.HasValue)
        {
            membersQuery = membersQuery.Where(m => m.GroupId == groupId.Value);
        }

        var members = await membersQuery
            .OrderBy(m => m.FirstName)
            .ThenBy(m => m.LastName)
            .Select(m => new MemberResponse(
                m.Id,
                m.Email,
                m.FirstName,
                m.LastName,
                m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName,
                m.GroupRole,
                m.GroupId,
                m.IsActive,
                dbContext.Users.Any(u => u.Id == m.UserId && u.IsActive),
                m.PhoneNumber,
                m.Address,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(members);
    }
}
