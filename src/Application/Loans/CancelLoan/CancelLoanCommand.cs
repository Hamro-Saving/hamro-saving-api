using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.CancelLoan;

public sealed record CancelLoanCommand(Guid LoanId) : ICommand;
