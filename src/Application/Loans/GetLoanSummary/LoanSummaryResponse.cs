using HamroSavings.Domain.Loans;

namespace HamroSavings.Application.Loans.GetLoanSummary;

public sealed record LoanSummaryResponse(
    int TotalLoans,
    int ActiveLoans,
    int PaidOffLoans,
    int OverdueLoans,
    decimal TotalPrincipal,
    decimal TotalInterest,
    decimal TotalDue,
    decimal TotalPaid,
    decimal TotalOutstanding);
