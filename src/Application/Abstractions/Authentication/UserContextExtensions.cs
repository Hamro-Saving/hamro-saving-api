using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;

namespace HamroSavings.Application.Abstractions.Authentication;

public static class UserContextExtensions
{
    /// <summary>
    /// The group an action belongs to. Admins and members always act inside the group on their
    /// token, so a group id in the request is ignored for them and cannot be spoofed. Only a
    /// SuperAdmin — who belongs to no group — names the group explicitly.
    /// </summary>
    public static Result<Guid> ResolveGroupId(this IUserContext userContext, Guid? requestedGroupId)
    {
        var groupId = userContext.IsSuperAdmin ? requestedGroupId : userContext.GroupId;

        return groupId is null || groupId == Guid.Empty
            ? Result.Failure<Guid>(UserErrors.NotInGroup)
            : Result.Success(groupId.Value);
    }
}
