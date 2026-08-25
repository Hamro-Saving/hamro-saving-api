using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.CompleteDisbursement;

/// <param name="DisbursedOn">
/// The day the money reached the borrower. Null means today — the ordinary case — while a past
/// date lets a loan the group made earlier be entered with its interest running from then.
/// </param>
public sealed record CompleteDisbursementCommand(Guid LoanId, DateOnly? DisbursedOn = null) : ICommand;
