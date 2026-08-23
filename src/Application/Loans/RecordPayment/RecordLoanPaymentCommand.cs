using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.RecordPayment;

/// <summary>
/// The split is the admin's call; the loan checks it against the interest actually accrued to
/// <paramref name="PaidDate"/> and derives the payment type from it.
/// </summary>
public sealed record RecordLoanPaymentCommand(
    Guid LoanId,
    Guid? GroupId,
    decimal PrincipalAmount,
    decimal InterestAmount,
    DateTime PaidDate,
    string? Notes) : ICommand<Guid>;
