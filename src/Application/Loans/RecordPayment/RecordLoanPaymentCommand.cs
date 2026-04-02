using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;

namespace HamroSavings.Application.Loans.RecordPayment;

public sealed record RecordLoanPaymentCommand(
    Guid LoanId,
    Guid GroupId,
    decimal Amount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    DateTime PaidDate,
    LoanPaymentType PaymentType,
    string? Notes) : ICommand<Guid>;
