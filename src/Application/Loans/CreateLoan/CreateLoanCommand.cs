using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.CreateLoan;

public sealed record CreateLoanCommand(
    Guid BorrowerId,
    string BorrowerType,
    Guid GroupId,
    decimal Amount,
    decimal? InterestRate,
    DateTime StartDate,
    DateTime? DueDate,
    string? Notes) : ICommand<Guid>;
