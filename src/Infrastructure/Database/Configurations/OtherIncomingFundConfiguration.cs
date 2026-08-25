using HamroSavings.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class OtherIncomingFundConfiguration : IEntityTypeConfiguration<OtherIncomingFund>
{
    public void Configure(EntityTypeBuilder<OtherIncomingFund> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(x => x.PaidDate).IsRequired();
        builder.Property(x => x.Remarks).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.GroupId, x.PaidDate });
        builder.HasIndex(x => x.MemberId);
    }
}
