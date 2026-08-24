using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Abstractions.Authentication;

/// <summary>
/// The caller, on two independent axes. <see cref="IsSuperAdmin"/> is the platform axis and says
/// nothing about any group; the Active* properties describe the one group the caller is currently
/// acting inside, chosen at login or via auth/switch-group.
/// </summary>
public interface IUserContext
{
    Guid UserId { get; }

    /// <summary>Platform administrator. Independent of group membership — a SuperAdmin may also be a group admin or member.</summary>
    bool IsSuperAdmin { get; }

    Guid? ActiveGroupId { get; }
    Guid? ActiveMemberId { get; }
    GroupRole? ActiveGroupRole { get; }

    /// <summary>Admin of the group currently being acted in.</summary>
    bool IsGroupAdmin { get; }

    /// <summary>Has any membership of the group currently being acted in, in any role.</summary>
    bool IsGroupMember { get; }

    /// <summary>Takes part in the group rather than only borrowing from it.</summary>
    bool ParticipatesInGroup { get; }

    /// <summary>Every group this person belongs to.</summary>
    IReadOnlyList<MembershipClaim> Memberships { get; }
}
