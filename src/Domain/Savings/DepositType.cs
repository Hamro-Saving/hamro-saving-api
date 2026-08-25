namespace HamroSavings.Domain.Savings;

public enum DepositType
{
    MonthlyDeposit = 0,

    /// <summary>Historical only. Loan interest is recorded against the loan, not as a deposit.</summary>
    InterestPayment = 1,

    /// <summary>Historical only. Repayments are recorded against the loan, not as a deposit.</summary>
    LoanRepayment = 2,

    Other = 3
}

public static class DepositTypeExtensions
{
    /// <summary>
    /// Whether a deposit of this kind may still be recorded. Interest and loan repayments
    /// belong to the loan's own payment flow, which splits principal from interest and posts
    /// each to its own account; booking one here would credit member savings instead, leaving
    /// the books saying the group owes the money back. Existing rows keep their type.
    /// </summary>
    public static bool CanBeRecorded(this DepositType type) =>
        type is DepositType.MonthlyDeposit or DepositType.Other;
}
