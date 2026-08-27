using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.ForceDisburseLoan;

/// <param name="DisbursedOn">The day the money reached the borrower; null means today.</param>
/// <param name="DisbursedAmount">What actually left the group; null means the full amount.</param>
public sealed record ForceDisburseLoanCommand(Guid LoanId, DateOnly? DisbursedOn = null, decimal? DisbursedAmount = null) : ICommand;
