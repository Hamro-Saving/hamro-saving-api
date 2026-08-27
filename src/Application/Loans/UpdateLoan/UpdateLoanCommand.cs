using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.UpdateLoan;

public sealed record UpdateLoanCommand(
    Guid LoanId,
    decimal Amount,
    decimal? InterestRate,
    DateTime StartDate,
    DateTime? DueDate,
    string? Notes) : ICommand;
