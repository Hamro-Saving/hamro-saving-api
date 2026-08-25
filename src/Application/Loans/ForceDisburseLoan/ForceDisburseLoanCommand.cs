using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.ForceDisburseLoan;

/// <param name="DisbursedOn">The day the money reached the borrower; null means today.</param>
public sealed record ForceDisburseLoanCommand(Guid LoanId, DateOnly? DisbursedOn = null) : ICommand;
