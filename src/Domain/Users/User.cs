using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Users;

public sealed class User : Entity
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? MemberId { get; private set; }
    public Guid? InviteToken { get; private set; }
    public DateTime? InviteTokenExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    /// <summary>Creates a SuperAdmin user with full profile info.</summary>
    public static User CreateSuperAdmin(
        string email,
        string passwordHash)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = UserRole.SuperAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.Raise(new UserRegisteredDomainEvent(user.Id));
        return user;
    }

    /// <summary>Creates an inactive user linked to a Member, pending invite acceptance.</summary>
    public static User CreateForMember(string email, Guid memberId)
    {
        return new User
        {
            Id = Guid.CreateVersion7(),
            Email = email.ToLowerInvariant(),
            Role = UserRole.Member,
            IsActive = false,
            MemberId = memberId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Creates an already-active user linked to an existing Member (used in data migrations).</summary>
    public static User CreateLinked(Guid existingId, string email, Guid memberId, DateTime createdAt)
    {
        return new User
        {
            Id = existingId,
            Email = email.ToLowerInvariant(),
            Role = UserRole.Member,
            IsActive = true,
            MemberId = memberId,
            CreatedAt = createdAt
        };
    }

    public Guid GenerateInviteToken(TimeSpan? expiry = null)
    {
        InviteToken = Guid.NewGuid();
        InviteTokenExpiresAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(72));
        return InviteToken.Value;
    }

    public void AcceptInvite(string passwordHash)
    {
        PasswordHash = passwordHash;
        IsActive = true;
        InviteToken = null;
        InviteTokenExpiresAt = null;
    }

    public void UpdateEmail(string email) => Email = email.ToLowerInvariant();
    public void UpdatePasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public void ChangeRole(UserRole role) => Role = role;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
