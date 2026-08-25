namespace HamroSavings.Domain.Ledger;

/// <summary>
/// The group's chart of accounts. Deliberately small: every movement of money the app
/// knows about lands in one of these.
/// </summary>
public enum LedgerAccount
{
    /// <summary>Asset. Money the group is actually holding.</summary>
    Cash = 0,

    /// <summary>Liability. Savings the group owes back to its members.</summary>
    MemberSavings = 1,

    /// <summary>Asset. Principal currently out with borrowers.</summary>
    LoanReceivable = 2,

    /// <summary>Income. Interest earned on loans and fixed deposits.</summary>
    InterestIncome = 3,

    /// <summary>Asset. Money parked with an institution.</summary>
    FixedDeposits = 4,

    /// <summary>Expense. Money spent and not coming back.</summary>
    Expenses = 5,
}

public static class LedgerAccountExtensions
{
    /// <summary>
    /// Assets and expenses grow on the debit side; liabilities and income grow on the
    /// credit side. Used to turn raw debit/credit totals into a balance that reads the
    /// way a person expects.
    /// </summary>
    public static bool IsDebitBalance(this LedgerAccount account) => account switch
    {
        LedgerAccount.Cash => true,
        LedgerAccount.LoanReceivable => true,
        LedgerAccount.FixedDeposits => true,
        LedgerAccount.Expenses => true,
        LedgerAccount.MemberSavings => false,
        LedgerAccount.InterestIncome => false,
        _ => true,
    };
}
