using HamroSavings.Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(g => g.Code)
            .IsUnique();

        builder.Property(g => g.Description)
            .HasMaxLength(1000);

        builder.Property(g => g.MemberInterestRate)
            .HasPrecision(5, 2)
            .HasDefaultValue(10m);

        builder.Property(g => g.NonMemberInterestRate)
            .HasPrecision(5, 2)
            .HasDefaultValue(18m);

        builder.Property(g => g.IsActive)
            .HasDefaultValue(true);

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.Property(g => g.UpdatedAt)
            .IsRequired();
    }
}
