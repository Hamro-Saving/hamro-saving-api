using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Members;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Auth;

/// <summary>One group a person belongs to, with the group loaded so validity can be judged.</summary>
internal sealed record LoadedMembership(Member Member, Group Group)
{
    public MembershipClaim ToClaim() =>
        new(Group.Id, Group.Name, Member.Id, Member.GroupRole);
}

internal static class MembershipLoader
{
    /// <summary>
    /// Every active membership this person holds, oldest first. Deactivating anyone — member
    /// or non-member — stops their membership being loaded, so the token stops carrying it.
    /// </summary>
    public static async Task<List<LoadedMembership>> LoadMembershipsAsync(
        this IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await (from m in dbContext.Members
                      join g in dbContext.Groups on m.GroupId equals g.Id
                      where m.UserId == userId && m.IsActive
                      orderby m.CreatedAt
                      select new LoadedMembership(m, g))
            .ToListAsync(cancellationToken);
    }

    /// <summary>A group is usable when it is active and inside its validity window.</summary>
    public static Result CheckUsable(Group group)
    {
        if (!group.IsActive)
            return Result.Failure(GroupErrors.NotActive);

        var now = DateTime.UtcNow;

        if (group.ValidFrom.HasValue && now < group.ValidFrom.Value)
            return Result.Failure(GroupErrors.ValidityNotStarted);

        if (group.ValidTo.HasValue && now > group.ValidTo.Value)
            return Result.Failure(GroupErrors.ValidityExpired);

        return Result.Success();
    }
}
