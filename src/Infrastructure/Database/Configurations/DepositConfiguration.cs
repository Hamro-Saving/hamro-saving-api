using HamroSavings.Domain.Savings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class DepositConfiguration : IEntityTypeConfiguration<Deposit>
{
    public void Configure(EntityTypeBuilder<Deposit> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(d => d.DepositMonth)
            .IsRequired();

        builder.Property(d => d.DepositYear)
            .IsRequired();

        builder.Property(d => d.DepositDate)
            .IsRequired();

        builder.Property(d => d.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(d => d.Notes)
            .HasMaxLength(500);

        builder.Property(d => d.IsVerified)
            .HasDefaultValue(false);

        builder.Property(d => d.CreatedAt)
            .IsRequired();
    }
}
