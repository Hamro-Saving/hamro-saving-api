using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.ApproveLoan;

public sealed record ApproveLoanCommand(Guid LoanId) : ICommand;
