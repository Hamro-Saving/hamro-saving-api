using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Ledger;

public static class LedgerErrors
{
    public static readonly Error AmountNotPositive =
        Error.Validation("Ledger.AmountNotPositive", "A ledger entry must have a positive amount.");

    /// <summary>The group cannot commit money it is not holding.</summary>
    public static Error InsufficientCash(decimal requested, decimal available) =>
        Error.Validation(
            "Ledger.InsufficientCash",
            $"The group has {available:N0} in hand, which is not enough for {requested:N0}.");

    public static readonly Error SameAccount =
        Error.Validation("Ledger.SameAccount", "A ledger entry must move value between two different accounts.");
}
