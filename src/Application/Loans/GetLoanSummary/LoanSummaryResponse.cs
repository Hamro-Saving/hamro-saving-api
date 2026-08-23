namespace HamroSavings.Application.Loans.GetLoanSummary;

public sealed record LoanSummaryResponse(
    int TotalLoans,
    int ActiveLoans,
    int PaidOffLoans,
    int OverdueLoans,
    /// <summary>Principal originally lent out across every loan.</summary>
    decimal TotalPrincipal,
    /// <summary>Principal still owed on live loans.</summary>
    decimal TotalOutstandingPrincipal,
    /// <summary>Interest run to today on live loans and not yet paid.</summary>
    decimal TotalAccruedInterest,
    decimal TotalPrincipalPaid,
    decimal TotalInterestPaid,
    /// <summary>What it would take to clear every live loan today.</summary>
    decimal TotalOutstanding);
