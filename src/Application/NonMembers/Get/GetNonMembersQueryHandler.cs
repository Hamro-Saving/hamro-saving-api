using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.NonMembers.Get;

internal sealed class GetNonMembersQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetNonMembersQuery, List<NonMemberResponse>>
{
    public async Task<Result<List<NonMemberResponse>>> Handle(GetNonMembersQuery query, CancellationToken cancellationToken = default)
    {
        var nonMembersQuery = dbContext.NonMembers.AsQueryable();

        if (!userContext.IsSuperAdmin)
        {
            var groupId = userContext.GroupId;
            nonMembersQuery = nonMembersQuery.Where(nm => nm.GroupId == groupId);
        }
        else if (query.GroupId.HasValue)
        {
            nonMembersQuery = nonMembersQuery.Where(nm => nm.GroupId == query.GroupId.Value);
        }

        var nonMembers = await nonMembersQuery
            .OrderBy(nm => nm.FullName)
            .Select(nm => new NonMemberResponse(
                nm.Id,
                nm.FullName,
                nm.Email,
                nm.Phone,
                nm.Address,
                nm.GroupId,
                nm.IsActive,
                nm.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(nonMembers);
    }
}
