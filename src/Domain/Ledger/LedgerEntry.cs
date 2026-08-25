using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Ledger;

/// <summary>
/// One double-entry line: value leaving one account and arriving in another, in equal
/// measure. Because every entry names both sides, the books prove themselves — total
/// debits and total credits must always agree, and any single entry that fails to
/// balance is impossible to express here.
///
/// Entries are written alongside the records they describe (a deposit, a payment, an
/// expense) and are never edited afterwards. A correction is a new, opposite entry.
/// </summary>
public sealed class LedgerEntry : Entity
{
    public Guid Id { get; private set; }
    public Guid GroupId { get; private set; }

    /// <summary>When the money actually moved, which is not always when the row was written.</summary>
    public DateTime OccurredAt { get; private set; }

    public TransactionType Type { get; private set; }
    public LedgerAccount DebitAccount { get; private set; }
    public LedgerAccount CreditAccount { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;

    /// <summary>The person the movement concerns, where there is one.</summary>
    public Guid? MemberId { get; private set; }

    /// <summary>The record that caused this entry, so the ledger can be traced back.</summary>
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private LedgerEntry() { }

    public static Result<LedgerEntry> Post(
        Guid groupId,
        DateTime occurredAt,
        TransactionType type,
        LedgerAccount debit,
        LedgerAccount credit,
        decimal amount,
        string description,
        string sourceType,
        Guid sourceId,
        Guid? memberId = null)
    {
        if (amount <= 0)
            return Result.Failure<LedgerEntry>(LedgerErrors.AmountNotPositive);

        if (debit == credit)
            return Result.Failure<LedgerEntry>(LedgerErrors.SameAccount);

        return Result.Success(new LedgerEntry
        {
            Id = Guid.CreateVersion7(),
            GroupId = groupId,
            OccurredAt = occurredAt,
            Type = type,
            DebitAccount = debit,
            CreditAccount = credit,
            Amount = amount,
            Description = description,
            MemberId = memberId,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
