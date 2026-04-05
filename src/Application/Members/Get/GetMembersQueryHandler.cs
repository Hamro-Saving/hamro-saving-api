using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;
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
        IQueryable<Member> membersQuery;

        if (query.MembershipType == MembershipType.NonMember)
        {
            membersQuery = dbContext.Members.Where(m => m.MembershipType == MembershipType.NonMember);
        }
        else
        {
            membersQuery = query.IncludeAdmins
                ? dbContext.Members.Where(m => m.MembershipType == MembershipType.Member)
                : dbContext.Members.Where(m => m.MembershipType == MembershipType.Member &&
                      !dbContext.Users.Any(u => u.MemberId == m.Id && u.Role == UserRole.Admin));
        }

        if (!userContext.IsSuperAdmin)
        {
            var groupId = userContext.GroupId;
            membersQuery = membersQuery.Where(m => m.GroupId == groupId);
        }
        else if (query.GroupId.HasValue)
        {
            membersQuery = membersQuery.Where(m => m.GroupId == query.GroupId.Value);
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
                dbContext.Users.Where(u => u.MemberId == m.Id).Select(u => (UserRole?)u.Role).FirstOrDefault(),
                m.MembershipType,
                m.GroupId,
                m.IsActive,
                dbContext.Users.Any(u => u.MemberId == m.Id && u.IsActive),
                m.PhoneNumber,
                m.Address,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(members);
    }
}
