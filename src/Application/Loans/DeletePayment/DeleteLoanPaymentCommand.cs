using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.DeletePayment;

public sealed record DeleteLoanPaymentCommand(Guid PaymentId) : ICommand;
