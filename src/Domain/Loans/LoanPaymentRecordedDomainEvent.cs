using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

/// <summary>
/// A repayment has been entered against a loan but not yet verified. Like a deposit, this
/// tells the admins there is something to verify and says nothing to the group yet.
/// </summary>
public sealed record LoanPaymentRecordedDomainEvent(Guid PaymentId, Guid LoanId) : IDomainEvent;
