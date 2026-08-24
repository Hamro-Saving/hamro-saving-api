using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;

namespace HamroSavings.Application.Abstractions.Authentication;

public static class UserContextExtensions
{
    /// <summary>
    /// The group a mutation belongs to: always the caller's active group. A group id in the request
    /// is ignored and cannot be spoofed, and there is no SuperAdmin escape — writing to a group's
    /// data requires a real membership in it, whatever the caller's platform role.
    /// </summary>
    public static Result<Guid> ResolveWriteGroupId(this IUserContext userContext)
    {
        var groupId = userContext.ActiveGroupId;

        return groupId is null || groupId == Guid.Empty
            ? Result.Failure<Guid>(UserErrors.NoActiveGroup)
            : Result.Success(groupId.Value);
    }

    /// <summary>
    /// The group an administrative write lands in — provisioning people and group settings, never
    /// financial data. A SuperAdmin may name a group here, which is how a freshly created group
    /// gets its first admin; everyone else is pinned to their active group.
    /// </summary>
    public static Result<Guid> ResolveAdminWriteGroupId(this IUserContext userContext, Guid? requestedGroupId)
    {
        var groupId = userContext.IsSuperAdmin && requestedGroupId is not null && requestedGroupId != Guid.Empty
            ? requestedGroupId
            : userContext.ActiveGroupId;

        return groupId is null || groupId == Guid.Empty
            ? Result.Failure<Guid>(UserErrors.NoActiveGroup)
            : Result.Success(groupId.Value);
    }

    /// <summary>
    /// The group a query is scoped to. A SuperAdmin may name any group, or none for a cross-group
    /// read; everyone else is pinned to their active group regardless of what they ask for.
    /// </summary>
    public static Result<Guid?> ResolveReadGroupId(this IUserContext userContext, Guid? requestedGroupId)
    {
        if (userContext.IsSuperAdmin)
            return Result.Success(requestedGroupId == Guid.Empty ? null : requestedGroupId);

        var groupId = userContext.ActiveGroupId;

        return groupId is null || groupId == Guid.Empty
            ? Result.Failure<Guid?>(UserErrors.NoActiveGroup)
            : Result.Success<Guid?>(groupId.Value);
    }

    /// <summary>
    /// Guards an administrative action on a group — managing its people or its settings. Allowed
    /// for an admin of that group, or for a SuperAdmin acting in their platform capacity.
    /// </summary>
    public static Result EnsureCanAdminister(this IUserContext userContext, Guid groupId) =>
        userContext.IsSuperAdmin || (userContext.IsGroupAdmin && userContext.ActiveGroupId == groupId)
            ? Result.Success()
            : Result.Failure(UserErrors.NotAGroupAdmin);

    /// <summary>
    /// A non-member borrows from the group without joining it, so they may see their own
    /// records and nothing else of the group's book. SuperAdmins and participants are unaffected.
    /// </summary>
    public static bool SeesOnlyOwnRecords(this IUserContext userContext) =>
        !userContext.IsSuperAdmin
        && userContext.ActiveGroupId is not null
        && !userContext.ParticipatesInGroup;

    /// <summary>
    /// Whether the caller may read data belonging to <paramref name="groupId"/>. SuperAdmins read
    /// across every group; everyone else only their active one.
    /// </summary>
    public static bool CanRead(this IUserContext userContext, Guid groupId) =>
        userContext.IsSuperAdmin || userContext.ActiveGroupId == groupId;

    /// <summary>
    /// Whether the caller may mutate data belonging to <paramref name="groupId"/>. Requires real
    /// membership — a SuperAdmin with no membership there may not.
    /// </summary>
    public static bool CanWrite(this IUserContext userContext, Guid groupId) =>
        userContext.ActiveGroupId == groupId;
}
