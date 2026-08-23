using HamroSavings.Domain.Loans;

namespace HamroSavings.Application.Loans.GetPayments;

public sealed record LoanPaymentResponse(
    Guid Id,
    Guid LoanId,
    decimal Amount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    DateTime PaidDate,
    LoanPaymentType PaymentType,
    string? Notes,
    // The interest calculation this payment settled
    decimal InterestOwedBefore,
    int DaysAccrued,
    decimal OutstandingPrincipalAfter,
    decimal UnpaidInterestAfter,
    bool IsVerified,
    DateTime? VerifiedAt,
    DateTime CreatedAt);
