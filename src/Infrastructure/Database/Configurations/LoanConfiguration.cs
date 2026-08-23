using HamroSavings.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BorrowerType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(l => l.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(l => l.InterestRate)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(l => l.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.OutstandingPrincipal).IsRequired().HasPrecision(18, 2);
        builder.Property(l => l.UnpaidInterest).IsRequired().HasPrecision(18, 2);
        builder.Property(l => l.TotalInterestAccrued).IsRequired().HasPrecision(18, 2);
        builder.Property(l => l.TotalInterestPaid).IsRequired().HasPrecision(18, 2);
        builder.Property(l => l.TotalPrincipalPaid).IsRequired().HasPrecision(18, 2);

        // Interest between transactions is computed, never stored
        builder.Ignore(l => l.DailyInterest);
    }
}
