using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.VerifyLoan;

public sealed record VerifyLoanCommand(Guid LoanId) : ICommand;
