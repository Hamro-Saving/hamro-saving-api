using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.UpdatePayment;

/// <summary>
/// Corrects a payment that has not been verified. As when it was recorded, the split is the
/// admin's call and the loan settles it against the interest accrued to <paramref name="PaidDate"/>.
/// </summary>
public sealed record UpdateLoanPaymentCommand(
    Guid PaymentId,
    decimal PrincipalAmount,
    decimal InterestAmount,
    DateTime PaidDate,
    string? Notes) : ICommand;
