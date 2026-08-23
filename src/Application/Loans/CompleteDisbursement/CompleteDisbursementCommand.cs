using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.CompleteDisbursement;

public sealed record CompleteDisbursementCommand(Guid LoanId) : ICommand;
