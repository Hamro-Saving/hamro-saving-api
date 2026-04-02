using HamroSavings.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class FixedDepositConfiguration : IEntityTypeConfiguration<FixedDeposit>
{
    public void Configure(EntityTypeBuilder<FixedDeposit> builder)
    {
        builder.HasKey(fd => fd.Id);

        builder.Property(fd => fd.InstitutionName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fd => fd.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(fd => fd.InterestRate)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(fd => fd.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(fd => fd.Notes)
            .HasMaxLength(500);

        builder.Property(fd => fd.CreatedAt)
            .IsRequired();

        builder.Ignore(fd => fd.ExpectedMaturityAmount);
    }
}
