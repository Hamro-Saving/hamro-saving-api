using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Members;

/// <summary>
/// One person's membership of one group. This is the join between a <c>User</c> and a <c>Group</c>,
/// and it carries the group axis of authorization (<see cref="GroupRole"/>). A person with rows in
/// several groups holds a different role in each. NonMembers may exist without a login at all.
/// </summary>
public sealed class Member : Entity
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string? LastName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Address { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid? UserId { get; private set; }
    public GroupRole GroupRole { get; private set; } = GroupRole.Member;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    public string FullName => string.IsNullOrEmpty(LastName) ? FirstName : $"{FirstName} {LastName}";

    private Member() { }

    public static Member Create(
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        Guid groupId,
        GroupRole groupRole = GroupRole.Member,
        string? address = null)
    {
        var member = new Member
        {
            Id = Guid.CreateVersion7(),
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            PhoneNumber = phoneNumber,
            Address = address,
            GroupId = groupId,
            GroupRole = groupRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        member.Raise(new MemberCreatedDomainEvent(member.Id));
        return member;
    }

    public static Member CreateNonMember(
        string fullName,
        string? email,
        string? phoneNumber,
        string? address,
        Guid groupId)
    {
        return new Member
        {
            Id = Guid.CreateVersion7(),
            FirstName = fullName,
            LastName = null,
            Email = email?.ToLowerInvariant(),
            PhoneNumber = phoneNumber,
            Address = address,
            GroupId = groupId,
            GroupRole = GroupRole.NonMember,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string firstName, string? lastName, string? email, string? phoneNumber, string? address = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email?.ToLowerInvariant();
        PhoneNumber = phoneNumber;
        Address = address;
    }

    public void LinkUser(Guid userId) => UserId = userId;
    public void UnlinkUser() => UserId = null;
    public void ChangeGroupRole(GroupRole role) => GroupRole = role;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
