using HamroSavings.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class LoanApprovalConfiguration : IEntityTypeConfiguration<LoanApproval>
{
    public void Configure(EntityTypeBuilder<LoanApproval> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.LoanId).IsRequired();
        builder.Property(a => a.ApproverId).IsRequired();
        builder.Property(a => a.ApprovedAt).IsRequired();

        builder.HasIndex(a => new { a.LoanId, a.ApproverId }).IsUnique();
    }
}
