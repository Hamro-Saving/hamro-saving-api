using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Ledger;

/// <summary>
/// What the group is holding, and the rule about spending it: money can only be committed
/// up to the balance, never beyond. The figure is read from the books by the caller; the
/// rule about what may be done with it lives here, so every path that commits money —
/// an expense, a fixed deposit, a loan applied for or disbursed — asks the same question.
/// </summary>
public readonly record struct CashInHand(decimal Amount)
{
    public bool Covers(decimal requested) => requested <= Amount;

    public Result EnsureCovers(decimal requested) =>
        Covers(requested)
            ? Result.Success()
            : Result.Failure(LedgerErrors.InsufficientCash(requested, Amount));
}
