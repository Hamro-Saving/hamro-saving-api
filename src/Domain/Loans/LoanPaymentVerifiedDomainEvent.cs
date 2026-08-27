using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

/// <summary>A repayment has been verified and posted to the books.</summary>
public sealed record LoanPaymentVerifiedDomainEvent(Guid PaymentId, Guid LoanId) : IDomainEvent;
