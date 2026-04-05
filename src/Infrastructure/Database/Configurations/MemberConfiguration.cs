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

        builder.Property(m => m.MembershipType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.IsActive)
            .HasDefaultValue(true);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Ignore(m => m.FullName);
    }
}
