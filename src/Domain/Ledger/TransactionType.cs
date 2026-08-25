namespace HamroSavings.Domain.Ledger;

/// <summary>The business event a ledger entry records.</summary>
public enum TransactionType
{
    Deposit = 0,
    LoanDisbursement = 1,
    LoanPrincipalPayment = 2,
    LoanInterestPayment = 3,
    FixedDepositPlaced = 4,
    FixedDepositWithdrawal = 5,
    FixedDepositInterest = 6,
    Expense = 7,

    /// <summary>Interest a late-joining member paid to catch up with the group.</summary>
    LateJoinerInterest = 8,
}
