using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.Get;
using HamroSavings.Domain.Members;
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
        var membersQuery = dbContext.Members.Where(m => m.Id == query.MemberId);

        if (!userContext.IsSuperAdmin && userContext.GroupId.HasValue)
        {
            var groupId = userContext.GroupId;
            membersQuery = membersQuery.Where(m => m.GroupId == groupId);
        }

        var member = await membersQuery
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
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
            return Result.Failure<MemberResponse>(MemberErrors.NotFound(query.MemberId));

        return Result.Success(member);
    }
}
