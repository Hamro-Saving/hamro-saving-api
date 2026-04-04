using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Users;

public sealed class User : Entity
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public Guid? GroupId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    private User() { }

    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role,
        Guid? groupId = null)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            GroupId = groupId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.Raise(new UserRegisteredDomainEvent(user.Id));
        return user;
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void ChangeRole(UserRole newRole) => Role = newRole;
    public void AssignGroup(Guid groupId) => GroupId = groupId;
    public void UpdatePasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
