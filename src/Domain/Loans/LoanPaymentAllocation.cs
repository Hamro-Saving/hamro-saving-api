namespace HamroSavings.Domain.Loans;

/// <summary>
/// What a payment actually settled, captured at the moment it was applied so the interest
/// calculation behind it stays auditable.
/// </summary>
public sealed record LoanPaymentAllocation(
    decimal InterestOwedBefore,
    int DaysAccrued,
    decimal InterestPaid,
    decimal PrincipalPaid,
    decimal OutstandingPrincipalAfter,
    decimal UnpaidInterestAfter);
