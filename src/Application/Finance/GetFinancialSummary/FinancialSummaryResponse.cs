namespace HamroSavings.Application.Finance.GetFinancialSummary;

public sealed record FinancialSummaryResponse(
    decimal TotalSavingsCollected,
    decimal TotalOnLoan,
    decimal TotalInterestCollected,
    decimal TotalExpenses,
    decimal TotalFixedDeposits,
    decimal InHandCash);
