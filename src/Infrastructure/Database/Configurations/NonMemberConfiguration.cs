using HamroSavings.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class NonMemberConfiguration : IEntityTypeConfiguration<NonMember>
{
    public void Configure(EntityTypeBuilder<NonMember> builder)
    {
        builder.HasKey(nm => nm.Id);

        builder.Property(nm => nm.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(nm => nm.Email)
            .HasMaxLength(256);

        builder.Property(nm => nm.Phone)
            .HasMaxLength(20);

        builder.Property(nm => nm.Address)
            .HasMaxLength(500);

        builder.Property(nm => nm.IsActive)
            .HasDefaultValue(true);

        builder.Property(nm => nm.CreatedAt)
            .IsRequired();
    }
}
