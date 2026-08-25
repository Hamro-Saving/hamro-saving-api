using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Domain.Ledger;

namespace HamroSavings.Application.Ledger;

/// <summary>
/// The one place that decides which accounts each business event touches. Handlers call
/// these rather than composing debit/credit pairs themselves, so the mapping cannot drift
/// between the code that records a deposit and the code that reports on it.
///
/// Entries are added to the change tracker but not saved; the caller commits them in the
/// same transaction as the record they describe, so a payment and its ledger lines land
/// together or not at all.
/// </summary>
internal static class LedgerPosting
{
    /// <summary>Money in from a member. Cash rises; what the group owes them rises with it.</summary>
    public static void PostDeposit(this IApplicationDbContext db, Guid groupId, Guid depositId, Guid memberId, decimal amount, DateTime occurredAt, string description)
        => db.Add(groupId, occurredAt, TransactionType.Deposit, LedgerAccount.Cash, LedgerAccount.MemberSavings, amount, description, "Deposit", depositId, memberId);

    /// <summary>Money out to a borrower. Cash falls and becomes a receivable.</summary>
    public static void PostLoanDisbursement(this IApplicationDbContext db, Guid groupId, Guid loanId, Guid borrowerId, decimal amount, DateTime occurredAt, string description)
        => db.Add(groupId, occurredAt, TransactionType.LoanDisbursement, LedgerAccount.LoanReceivable, LedgerAccount.Cash, amount, description, "Loan", loanId, borrowerId);

    /// <summary>Principal coming back. Cash rises, the receivable shrinks; no income in it.</summary>
    public static void PostLoanPrincipal(this IApplicationDbContext db, Guid groupId, Guid paymentId, Guid borrowerId, decimal amount, DateTime occurredAt, string description)
        => db.Add(groupId, occurredAt, TransactionType.LoanPrincipalPayment, LedgerAccount.Cash, LedgerAccount.LoanReceivable, amount, description, "LoanPayment", paymentId, borrowerId);

    /// <summary>Interest on a loan. This part is earnings, not a return of capital.</summary>
    public static void PostLoanInterest(this IApplicationDbContext db, Guid groupId, Guid paymentId, Guid borrowerId, decimal amount, DateTime occurredAt, string description)
        => db.Add(groupId, occurredAt, TransactionType.LoanInterestPayment, LedgerAccount.Cash, LedgerAccount.InterestIncome, amount, description, "LoanPayment", paymentId, borrowerId);

    /// <summary>Cash parked with an institution. Still the group's money, just not in hand.</summary>
    public static void PostFixedDepositPlaced(this IApplicationDbContext db, Guid groupId, Guid fixedDepositId, decimal amount, DateTime occurredAt, string description)
        => db.Add(groupId, occurredAt, TransactionType.FixedDepositPlaced, LedgerAccount.FixedDeposits, LedgerAccount.Cash, amount, description, "FixedDeposit", fixedDepositId, null);

    /// <summary>The principal coming back out of the institution.</summary>
    public static void PostFixedDepositWithdrawal(this IApplicationDbContext db, Guid groupId, Guid fixedDepositId, decimal amount, DateTime occurredAt, string description)
        => db.Add(groupId, occurredAt, TransactionType.FixedDepositWithdrawal, LedgerAccount.Cash, LedgerAccount.FixedDeposits, amount, description, "FixedDeposit", fixedDepositId, null);

    /// <summary>What the institution paid on top, posted separately so income stays visible.</summary>
    public static void PostFixedDepositInterest(this IApplicationDbContext db, Guid groupId, Guid fixedDepositId, decimal amount, DateTime occurredAt, string description)
        => db.Add(groupId, occurredAt, TransactionType.FixedDepositInterest, LedgerAccount.Cash, LedgerAccount.InterestIncome, amount, description, "FixedDeposit", fixedDepositId, null);

    /// <summary>Money spent and gone.</summary>
    public static void PostExpense(this IApplicationDbContext db, Guid groupId, Guid expenseId, decimal amount, DateTime occurredAt, string description)
        => db.Add(groupId, occurredAt, TransactionType.Expense, LedgerAccount.Expenses, LedgerAccount.Cash, amount, description, "Expense", expenseId, null);

    private static void Add(
        this IApplicationDbContext db,
        Guid groupId,
        DateTime occurredAt,
        TransactionType type,
        LedgerAccount debit,
        LedgerAccount credit,
        decimal amount,
        string description,
        string sourceType,
        Guid sourceId,
        Guid? memberId)
    {
        // A zero or negative leg is not a movement of money, so nothing is written. This
        // keeps callers from having to special-case an interest-free payment.
        var entry = LedgerEntry.Post(groupId, occurredAt, type, debit, credit, amount, description, sourceType, sourceId, memberId);
        if (entry.IsSuccess)
            db.LedgerEntries.Add(entry.Value);
    }
}
