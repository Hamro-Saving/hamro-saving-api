using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Users;

/// <summary>
/// A login identity. Carries the platform axis of authorization only — what a person may do
/// inside a group lives on their <c>Member</c> row for that group, one per group.
/// </summary>
public sealed class User : Entity
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsSuperAdmin { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? InviteToken { get; private set; }
    public DateTime? InviteTokenExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    /// <summary>Creates a platform SuperAdmin. Belongs to no group until given a Member row.</summary>
    public static User CreateSuperAdmin(
        string email,
        string passwordHash)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            IsSuperAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.Raise(new UserRegisteredDomainEvent(user.Id));
        return user;
    }

    /// <summary>Creates an inactive login pending invite acceptance. Group membership is a separate Member row.</summary>
    public static User CreateMember(string email, string passwordHash)
    {
        return new User
        {
            Id = Guid.CreateVersion7(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            IsSuperAdmin = false,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Guid GenerateInviteToken(TimeSpan? expiry = null)
    {
        InviteToken = Guid.CreateVersion7();
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
    public void GrantSuperAdmin() => IsSuperAdmin = true;
    public void RevokeSuperAdmin() => IsSuperAdmin = false;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
