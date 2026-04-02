using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.NonMembers.Get;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.NonMembers.GetById;

internal sealed class GetNonMemberByIdQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetNonMemberByIdQuery, NonMemberResponse>
{
    public async Task<Result<NonMemberResponse>> Handle(GetNonMemberByIdQuery query, CancellationToken cancellationToken = default)
    {
        var nonMembersQuery = dbContext.NonMembers
            .Where(nm => nm.Id == query.NonMemberId);

        if (!userContext.IsSuperAdmin && userContext.GroupId.HasValue)
        {
            var groupId = userContext.GroupId;
            nonMembersQuery = nonMembersQuery.Where(nm => nm.GroupId == groupId);
        }

        var nonMember = await nonMembersQuery
            .Select(nm => new NonMemberResponse(
                nm.Id,
                nm.FullName,
                nm.Email,
                nm.Phone,
                nm.Address,
                nm.GroupId,
                nm.IsActive,
                nm.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (nonMember is null)
        {
            return Result.Failure<NonMemberResponse>(NonMemberErrors.NotFound(query.NonMemberId));
        }

        return Result.Success(nonMember);
    }
}
