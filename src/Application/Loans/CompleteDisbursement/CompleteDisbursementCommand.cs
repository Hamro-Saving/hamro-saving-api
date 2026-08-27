using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.CompleteDisbursement;

/// <param name="DisbursedOn">
/// The day the money reached the borrower. Null means today — the ordinary case — while a past
/// date lets a loan the group made earlier be entered with its interest running from then.
/// </param>
/// <param name="DisbursedAmount">
/// What actually left the group. Null means the full amount the loan was approved for.
/// </param>
public sealed record CompleteDisbursementCommand(Guid LoanId, DateOnly? DisbursedOn = null, decimal? DisbursedAmount = null) : ICommand;
