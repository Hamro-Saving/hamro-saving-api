using HamroSavings.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.FirstName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.LastName)
            .HasMaxLength(100);

        builder.Property(m => m.Email)
            .HasMaxLength(256);

        builder.HasIndex(m => new { m.Email, m.GroupId })
            .IsUnique()
            .HasFilter("email IS NOT NULL");

        builder.Property(m => m.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(m => m.Address)
            .HasMaxLength(500);

        builder.Property(m => m.UserId);

        // One membership per person per group. Nullable because a NonMember may have no login.
        builder.HasIndex(m => new { m.UserId, m.GroupId })
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");

        builder.HasOne<HamroSavings.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<HamroSavings.Domain.Groups.Group>()
            .WithMany()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // No store default: NonMember is 0, the CLR default for the enum, so EF would treat
        // it as "unset" and let the column default write 'Member' over it. The role is always
        // written explicitly; Member.GroupRole already defaults to Member in the domain.
        builder.Property(m => m.GroupRole)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.IsActive)
            .HasDefaultValue(true);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Ignore(m => m.FullName);
    }
}
