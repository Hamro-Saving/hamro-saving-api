using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Savings;
using System.Globalization;

namespace HamroSavings.Infrastructure.Email;

internal static class Money
{
    /// <summary>As the group's own books would write it: <c>Rs. 12,500.00</c>.</summary>
    public static string Format(decimal amount) =>
        string.Create(CultureInfo.InvariantCulture, $"Rs. {amount:N2}");
}

internal static class Footnotes
{
    /// <summary>
    /// Callers pass every remark they might make and let this drop the blanks, rather than
    /// each assembling the same conditional string by hand.
    /// </summary>
    public static string? Join(params string?[] remarks) =>
        string.Join(" ", remarks.Where(r => !string.IsNullOrWhiteSpace(r))) is { Length: > 0 } text
            ? text
            : null;
}

/// <summary>
/// Only two, because only two emails ask the reader to do something: sign up, and vote on a
/// loan. The rest report what happened, where a button would be furniture.
/// </summary>
internal static class EmailLinks
{
    public static string Loan(Guid loanId) => $"/loans/{loanId}";

    public static string Signup(Guid inviteToken) => $"/signup?token={inviteToken}";
}

/// <summary>In one place so every email describing a deposit agrees.</summary>
internal static class DepositNarrative
{
    /// <summary>A phrase that fits mid-sentence. Only a monthly deposit has a period, so only it names one.</summary>
    public static string Describe(Deposit deposit) => deposit.Type switch
    {
        DepositType.MonthlyDeposit =>
            $"monthly deposit of {Money.Format(deposit.Amount)} for {BikramSambat.Period(deposit.DepositMonth!.Value, deposit.DepositYear!.Value)}",
        DepositType.InterestPayment => $"interest payment of {Money.Format(deposit.Amount)}",
        DepositType.LoanRepayment => $"loan repayment of {Money.Format(deposit.Amount)}",
        _ => $"deposit of {Money.Format(deposit.Amount)}",
    };

    public static List<EmailDetail> Figures(Deposit deposit, string depositorName) =>
    [
        new("Member", depositorName),
        new("Amount", Money.Format(deposit.Amount)),
        .. deposit is { DepositMonth: { } month, DepositYear: { } year }
            ? new[] { new EmailDetail("For the month of", BikramSambat.Period(month, year)) }
            : [],
        new("Deposited on", deposit.DepositDate.ToString("dd MMM yyyy")),
        .. string.IsNullOrWhiteSpace(deposit.Notes) ? [] : new[] { new EmailDetail("Notes", deposit.Notes!) },
    ];
}

/// <summary>In one place so every email describing a loan agrees.</summary>
internal static class LoanNarrative
{
    /// <summary>A non-member is marked as one — lending outside the group is the most important fact about a request.</summary>
    public static string Borrower(Member borrower) =>
        borrower.GroupRole == GroupRole.NonMember ? $"{borrower.FullName} (non-member)" : borrower.FullName;

    public static List<EmailDetail> Terms(Loan loan, Member borrower) =>
    [
        new("Borrower", Borrower(borrower)),
        new("Amount", Money.Format(loan.Amount)),
        new("Interest rate", $"{loan.InterestRate:0.##}% per year"),
        new("Start date", loan.StartDate.ToString("dd MMM yyyy")),
        .. loan.DueDate is { } due ? new[] { new EmailDetail("Due date", due.ToString("dd MMM yyyy")) } : [],
        .. string.IsNullOrWhiteSpace(loan.Notes) ? [] : new[] { new EmailDetail("Notes", loan.Notes!) },
    ];
}

/// <summary>In one place so every email describing a repayment agrees.</summary>
internal static class LoanPaymentNarrative
{
    /// <summary>A payment that is all principal or all interest says so, rather than reciting a zero.</summary>
    public static string Describe(LoanPayment payment) => payment.PaymentType switch
    {
        LoanPaymentType.Principal => $"principal repayment of {Money.Format(payment.PrincipalAmount)}",
        LoanPaymentType.Interest => $"interest payment of {Money.Format(payment.InterestAmount)}",
        _ => $"loan payment of {Money.Format(payment.Amount)}",
    };

    /// <summary>
    /// Clearing the principal is the moment a borrower thinks they are finished and is wrong —
    /// the loan stays open until the interest is paid too.
    ///
    /// Nothing further accrues, since interest runs on outstanding principal and there is
    /// none, so the figure quoted is the whole of what is left.
    /// </summary>
    public static string? InterestStillOwed(LoanPayment payment) =>
        payment.OutstandingPrincipalAfter <= 0 && payment.UnpaidInterestAfter > 0
            ? $"The principal is now fully repaid, but {Money.Format(payment.UnpaidInterestAfter)} of interest is still owed. "
              + "The loan stays open until that is paid, though no further interest will accrue on it."
            : null;

    public static List<EmailDetail> Figures(LoanPayment payment, Loan loan, string borrowerLabel) =>
    [
        new("Borrower", borrowerLabel),
        new("Total paid", Money.Format(payment.Amount)),
        new("Towards principal", Money.Format(payment.PrincipalAmount)),
        new("Towards interest", Money.Format(payment.InterestAmount)),
        new("Paid on", payment.PaidDate.ToString("dd MMM yyyy")),
        new("Principal still owed", Money.Format(payment.OutstandingPrincipalAfter)),
        new("Unpaid interest", Money.Format(payment.UnpaidInterestAfter)),
        new("Interest rate", $"{loan.InterestRate:0.##}% per year"),
        .. string.IsNullOrWhiteSpace(payment.Notes) ? [] : new[] { new EmailDetail("Notes", payment.Notes!) },
    ];
}
