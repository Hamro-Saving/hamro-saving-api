using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Domain.Members;

namespace UnitTests.Users;

/// <summary>
/// Authorization has two independent axes: SuperAdmin is about the platform and grants nothing
/// inside any group, while GroupRole is about one group and grants nothing platform-wide. The
/// same person can hold both, and a different group role in each group they belong to.
/// </summary>
public class RoleAxesTests
{
    private static readonly Guid GroupA = Guid.NewGuid();
    private static readonly Guid GroupB = Guid.NewGuid();

    [Fact]
    public void APersonCanBeSuperAdminAdminOfOneGroupAndMemberOfAnother()
    {
        var userId = Guid.NewGuid();
        var memberships = new List<MembershipClaim>
        {
            new(GroupA, "Group A", Guid.NewGuid(), GroupRole.Admin),
            new(GroupB, "Group B", Guid.NewGuid(), GroupRole.Member)
        };

        var inA = new FakeUserContext
        {
            UserId = userId,
            IsSuperAdmin = true,
            ActiveGroupId = GroupA,
            ActiveGroupRole = GroupRole.Admin,
            Memberships = memberships
        };

        var inB = new FakeUserContext
        {
            UserId = userId,
            IsSuperAdmin = true,
            ActiveGroupId = GroupB,
            ActiveGroupRole = GroupRole.Member,
            Memberships = memberships
        };

        // One identity, three roles, each answering for its own scope.
        Assert.True(inA.IsSuperAdmin);
        Assert.True(inA.IsGroupAdmin);
        Assert.True(inB.IsSuperAdmin);
        Assert.False(inB.IsGroupAdmin);
        Assert.True(inB.IsGroupMember);
    }

    [Fact]
    public void SuperAdminAloneCannotWriteToAnyGroup()
    {
        var platform = FakeUserContext.PlatformOnly();

        Assert.True(platform.ResolveWriteGroupId().IsFailure);
        Assert.False(platform.CanWrite(GroupA));
    }

    [Fact]
    public void SuperAdminAloneCanStillReadAcrossGroups()
    {
        var platform = FakeUserContext.PlatformOnly();

        Assert.True(platform.CanRead(GroupA));
        Assert.True(platform.CanRead(GroupB));

        var scoped = platform.ResolveReadGroupId(GroupA);
        Assert.True(scoped.IsSuccess);
        Assert.Equal(GroupA, scoped.Value);

        // No group named means every group.
        Assert.Null(platform.ResolveReadGroupId(null).Value);
    }

    [Fact]
    public void SuperAdminWithAMembershipWritesOnlyToThatGroup()
    {
        var both = FakeUserContext.In(GroupA, GroupRole.Admin, superAdmin: true);

        Assert.Equal(GroupA, both.ResolveWriteGroupId().Value);
        Assert.True(both.CanWrite(GroupA));
        Assert.False(both.CanWrite(GroupB));
    }

    [Fact]
    public void AGroupAdminCannotReachAnotherGroup()
    {
        var adminOfA = FakeUserContext.In(GroupA, GroupRole.Admin);

        Assert.False(adminOfA.CanWrite(GroupB));
        Assert.False(adminOfA.CanRead(GroupB));
        Assert.True(adminOfA.EnsureCanAdminister(GroupB).IsFailure);
        Assert.True(adminOfA.EnsureCanAdminister(GroupA).IsSuccess);
    }

    [Fact]
    public void AGroupAdminsRequestedGroupIdIsIgnoredOnReadsAndWrites()
    {
        var adminOfA = FakeUserContext.In(GroupA, GroupRole.Admin);

        Assert.Equal(GroupA, adminOfA.ResolveWriteGroupId().Value);
        Assert.Equal(GroupA, adminOfA.ResolveReadGroupId(GroupB).Value);
    }

    [Fact]
    public void APlainMemberCannotAdministerTheirOwnGroup()
    {
        var member = FakeUserContext.In(GroupA, GroupRole.Member);

        Assert.False(member.IsGroupAdmin);
        Assert.True(member.IsGroupMember);
        Assert.True(member.EnsureCanAdminister(GroupA).IsFailure);
    }

    [Fact]
    public void SuperAdminMayAdministerAGroupItDoesNotBelongTo()
    {
        // This is the bootstrap path: a freshly created group has no admin to appoint the first one.
        var platform = FakeUserContext.PlatformOnly();

        Assert.True(platform.EnsureCanAdminister(GroupA).IsSuccess);
        Assert.Equal(GroupA, platform.ResolveAdminWriteGroupId(GroupA).Value);
    }

    [Fact]
    public void AGroupAdminCannotProvisionIntoAnotherGroupByNamingIt()
    {
        var adminOfA = FakeUserContext.In(GroupA, GroupRole.Admin);

        Assert.Equal(GroupA, adminOfA.ResolveAdminWriteGroupId(GroupB).Value);
    }

    [Fact]
    public void ANonMemberBelongsToTheGroupButTakesNoPartInIt()
    {
        var borrower = FakeUserContext.In(GroupA, GroupRole.NonMember);

        // They are in the group — loans are theirs to hold — but they neither
        // deposit nor vote, and they certainly do not administer.
        Assert.True(borrower.IsGroupMember);
        Assert.False(borrower.ParticipatesInGroup);
        Assert.False(borrower.IsGroupAdmin);
        Assert.True(borrower.EnsureCanAdminister(GroupA).IsFailure);
    }

    [Fact]
    public void MembersAndAdminsBothParticipate()
    {
        Assert.True(FakeUserContext.In(GroupA, GroupRole.Member).ParticipatesInGroup);
        Assert.True(FakeUserContext.In(GroupA, GroupRole.Admin).ParticipatesInGroup);
    }

    [Fact]
    public void OnlyANonMemberIsLimitedToTheirOwnRecords()
    {
        Assert.True(FakeUserContext.In(GroupA, GroupRole.NonMember).SeesOnlyOwnRecords());
        Assert.False(FakeUserContext.In(GroupA, GroupRole.Member).SeesOnlyOwnRecords());
        Assert.False(FakeUserContext.In(GroupA, GroupRole.Admin).SeesOnlyOwnRecords());

        // A SuperAdmin reads across groups and is never narrowed to one member's rows.
        Assert.False(FakeUserContext.PlatformOnly().SeesOnlyOwnRecords());
        Assert.False(FakeUserContext.In(GroupA, GroupRole.NonMember, superAdmin: true).SeesOnlyOwnRecords());
    }

    [Fact]
    public void AGrouplessNonSuperAdminIsScopedToNothing()
    {
        var stranded = new FakeUserContext();

        Assert.True(stranded.ResolveReadGroupId(GroupA).IsFailure);
        Assert.True(stranded.ResolveWriteGroupId().IsFailure);
        Assert.False(stranded.CanRead(GroupA));
    }
}
