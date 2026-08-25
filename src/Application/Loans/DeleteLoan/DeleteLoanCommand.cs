using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.DeleteLoan;

public sealed record DeleteLoanCommand(Guid LoanId) : ICommand;
