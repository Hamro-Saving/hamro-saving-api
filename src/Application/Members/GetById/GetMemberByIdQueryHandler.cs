using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.Get;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Members.GetById;

internal sealed class GetMemberByIdQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetMemberByIdQuery, MemberResponse>
{
    public async Task<Result<MemberResponse>> Handle(GetMemberByIdQuery query, CancellationToken cancellationToken = default)
    {
        var membersQuery = dbContext.Users
            .Where(u => u.Id == query.MemberId && u.Role == UserRole.Member);

        if (!userContext.IsSuperAdmin && userContext.GroupId.HasValue)
        {
            var groupId = userContext.GroupId;
            membersQuery = membersQuery.Where(u => u.GroupId == groupId);
        }

        var member = await membersQuery
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
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
        {
            return Result.Failure<MemberResponse>(UserErrors.NotFound(query.MemberId));
        }

        return Result.Success(member);
    }
}
