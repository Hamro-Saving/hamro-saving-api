using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Ledger;

public static class LedgerErrors
{
    public static readonly Error AmountNotPositive =
        Error.Validation("Ledger.AmountNotPositive", "A ledger entry must have a positive amount.");

    public static readonly Error SameAccount =
        Error.Validation("Ledger.SameAccount", "A ledger entry must move value between two different accounts.");
}
