using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.Get;
using HamroSavings.Domain.Members;
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

        if (!userContext.IsSuperAdmin)
        {
            var groupId = userContext.ActiveGroupId;
            membersQuery = membersQuery.Where(m => m.GroupId == groupId);
        }

        var member = await membersQuery
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
                dbContext.Deposits
                    .Where(d => d.MemberId == m.Id && d.IsVerified)
                    .Sum(d => (decimal?)d.Amount) ?? 0,
                0m,
                0m,
                m.PhoneNumber,
                m.Address,
                m.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
            return Result.Failure<MemberResponse>(MemberErrors.NotFound(query.MemberId));

        var owed = await Get.GetMembersQueryHandler.LoansOwedBy(dbContext, [member.Id], cancellationToken);

        return Result.Success(owed.TryGetValue(member.Id, out var o)
            ? member with { OutstandingPrincipal = o.Principal, OutstandingInterest = o.Interest }
            : member);
    }
}
