using HamroSavings.Domain.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HamroSavings.Infrastructure.Database.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type).IsRequired().HasConversion<string>();
        builder.Property(e => e.DebitAccount).IsRequired().HasConversion<string>();
        builder.Property(e => e.CreditAccount).IsRequired().HasConversion<string>();

        builder.Property(e => e.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(300);
        builder.Property(e => e.SourceType).IsRequired().HasMaxLength(40);
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        // The ledger is read as a group's book, newest first.
        builder.HasIndex(e => new { e.GroupId, e.OccurredAt });

        // Tracing an entry back to what caused it, and finding a member's history.
        builder.HasIndex(e => new { e.SourceType, e.SourceId });
        builder.HasIndex(e => e.MemberId);

        // One posting per event per source: re-verifying a payment or replaying a
        // migration cannot double-count the money.
        builder.HasIndex(e => new { e.SourceType, e.SourceId, e.Type }).IsUnique();

        builder.HasOne<HamroSavings.Domain.Groups.Group>()
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
