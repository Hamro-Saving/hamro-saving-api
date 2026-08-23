using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.GetPayments;

public sealed record GetLoanPaymentsQuery(Guid LoanId) : IQuery<List<LoanPaymentResponse>>;
