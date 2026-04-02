using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.VerifyPayment;

public sealed record VerifyLoanPaymentCommand(Guid PaymentId, Guid GroupId) : ICommand;
