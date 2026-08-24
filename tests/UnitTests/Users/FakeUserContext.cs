using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Domain.Members;

namespace UnitTests.Users;

/// <summary>An in-memory caller, built the way the token builds one: two independent axes.</summary>
internal sealed class FakeUserContext : IUserContext
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public bool IsSuperAdmin { get; init; }
    public Guid? ActiveGroupId { get; init; }
    public Guid? ActiveMemberId { get; init; }
    public GroupRole? ActiveGroupRole { get; init; }
    public IReadOnlyList<MembershipClaim> Memberships { get; init; } = [];

    public bool IsGroupAdmin => ActiveGroupRole == GroupRole.Admin;
    public bool IsGroupMember => ActiveGroupId is not null;
    public bool ParticipatesInGroup => ActiveGroupRole?.Participates() == true;

    /// <summary>Acting inside <paramref name="groupId"/> with the given role.</summary>
    public static FakeUserContext In(Guid groupId, GroupRole role, bool superAdmin = false) =>
        new()
        {
            IsSuperAdmin = superAdmin,
            ActiveGroupId = groupId,
            ActiveMemberId = Guid.NewGuid(),
            ActiveGroupRole = role
        };

    /// <summary>A platform admin who belongs to no group at all.</summary>
    public static FakeUserContext PlatformOnly() => new() { IsSuperAdmin = true };
}
