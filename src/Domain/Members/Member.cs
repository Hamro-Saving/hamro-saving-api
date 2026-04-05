using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Members;

public sealed class Member : Entity
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string? LastName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Address { get; private set; }
    public Guid GroupId { get; private set; }
    public MembershipType MembershipType { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    public string FullName => string.IsNullOrEmpty(LastName) ? FirstName : $"{FirstName} {LastName}";

    private Member() { }

    public static Member Create(
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        Guid groupId)
    {
        var member = new Member
        {
            Id = Guid.CreateVersion7(),
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            PhoneNumber = phoneNumber,
            GroupId = groupId,
            MembershipType = MembershipType.Member,
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
            MembershipType = MembershipType.NonMember,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Creates a Member record for an existing User (used during data migration).</summary>
    public static Member CreateFromExistingUser(
        Guid existingId,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        Guid groupId,
        DateTime createdAt)
    {
        return new Member
        {
            Id = existingId,
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            PhoneNumber = phoneNumber,
            GroupId = groupId,
            MembershipType = MembershipType.Member,
            IsActive = true,
            CreatedAt = createdAt
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

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
