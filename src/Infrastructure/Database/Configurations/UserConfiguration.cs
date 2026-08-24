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

        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}
