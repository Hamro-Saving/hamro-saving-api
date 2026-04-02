using HamroSavings.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class LoanPaymentConfiguration : IEntityTypeConfiguration<LoanPayment>
{
    public void Configure(EntityTypeBuilder<LoanPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.PrincipalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.InterestAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.PaymentType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Property(p => p.IsVerified)
            .HasDefaultValue(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasOne<Loan>()
            .WithMany()
            .HasForeignKey(p => p.LoanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
