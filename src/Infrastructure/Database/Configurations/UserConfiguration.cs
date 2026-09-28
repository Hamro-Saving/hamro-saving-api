using HamroSavings.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.IsSuperAdmin)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(false);

        builder.HasIndex(u => u.InviteToken)
            .IsUnique()
            .HasFilter("invite_token IS NOT NULL");

        builder.Property(u => u.InviteToken);
        builder.Property(u => u.InviteTokenExpiresAt);

        // Looked up by token alone, the same way an invite is, so the index is what makes
        // that a seek rather than a scan of every account on the platform.
        builder.HasIndex(u => u.PasswordResetToken)
            .IsUnique()
            .HasFilter("password_reset_token IS NOT NULL");

        builder.Property(u => u.PasswordResetToken);
        builder.Property(u => u.PasswordResetTokenExpiresAt);
        builder.Property(u => u.PasswordResetRequestedAt);

        // Null until the first sign-in, which is the difference between an invite nobody has
        // taken up and an account in use.
        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}
