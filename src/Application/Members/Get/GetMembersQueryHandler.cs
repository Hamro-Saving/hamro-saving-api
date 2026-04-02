using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;
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
        var membersQuery = query.IncludeAdmins
            ? dbContext.Users.Where(u => u.Role == UserRole.Member || u.Role == UserRole.Admin)
            : dbContext.Users.Where(u => u.Role == UserRole.Member);

        if (!userContext.IsSuperAdmin)
        {
            var groupId = userContext.GroupId;
            membersQuery = membersQuery.Where(u => u.GroupId == groupId);
        }
        else if (query.GroupId.HasValue)
        {
            membersQuery = membersQuery.Where(u => u.GroupId == query.GroupId.Value);
        }

        var members = await membersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new MemberResponse(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.FirstName + " " + u.LastName,
                u.Role,
                u.GroupId,
                u.IsActive,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(members);
    }
}
