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
    public Guid? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }
    /// <summary>When a reset link was last <em>sent</em> — which is what the cooldown counts,
    /// whether or not the link it sent was ever used.</summary>
    public DateTime? PasswordResetRequestedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
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

    /// <summary>How long a reset link is good for once it has been sent.</summary>
    public static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    /// <summary>
    /// How long before the same address can be sent another link.
    ///
    /// This is the half of the throttling that rate limiting cannot do. A limit per caller
    /// stops one machine flooding the API; it cannot see many machines each asking once for
    /// the same person, which is how an inbox gets buried. Counted against the account, that
    /// flood is one request and the rest are refused.
    /// </summary>
    public static readonly TimeSpan ResetRequestCooldown = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Begins a password reset, or says why not. Far shorter-lived than an invite: an invite
    /// waits for someone to get around to joining, a reset answers a request made a minute
    /// ago, and a link that sets a password should not sit in a mailbox for days.
    ///
    /// Both refusals live here rather than in the handler that asks, so no later caller can
    /// mint a link around them. Neither is ever shown to whoever asked — that would say
    /// whether the address has an account — but the reason is worth having for the log.
    /// </summary>
    public Result<Guid> GeneratePasswordResetToken(DateTime now, TimeSpan? expiry = null)
    {
        // Never claimed, so the pending invite is what sets the first password; or disabled,
        // which must not be handed a way back in.
        if (!IsActive)
            return Result.Failure<Guid>(UserErrors.NotActive);

        if (PasswordResetRequestedAt is { } last && now - last < ResetRequestCooldown)
            return Result.Failure<Guid>(UserErrors.ResetAskedTooRecently);

        PasswordResetToken = Guid.CreateVersion7();
        PasswordResetRequestedAt = now;
        PasswordResetTokenExpiresAt = now.Add(expiry ?? ResetTokenLifetime);
        return Result.Success(PasswordResetToken.Value);
    }

    /// <summary>
    /// Spends the reset token as it sets the password, so one link sets one password and a
    /// second use of the same link is refused.
    /// </summary>
    public void ResetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
    }

    /// <summary>
    /// Notes that this person has just been let in. Every path that mints a token calls it,
    /// signing up included — that hands back a token too, so it is a sign-in like any other
    /// and a member who joined this morning should not read as never having appeared.
    /// </summary>
    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void UpdateEmail(string email) => Email = email.ToLowerInvariant();
    public void UpdatePasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public void GrantSuperAdmin() => IsSuperAdmin = true;
    public void RevokeSuperAdmin() => IsSuperAdmin = false;
    /// <summary>
    /// Disables the login. Any reset link already in flight dies with it — a closed account
    /// must not leave a live credential sitting in a mailbox, where it would outlast the
    /// decision to close it.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
    }
    public void Activate() => IsActive = true;
}
