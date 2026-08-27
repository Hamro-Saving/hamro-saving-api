using HamroSavings.Domain.Loans;

namespace HamroSavings.Application.Loans.ListPayments;

/// <summary>
/// A payment seen from outside its loan, for queues that span the whole group. Carries the
/// borrower and the loan it belongs to, which <see cref="GetPayments.LoanPaymentResponse"/>
/// leaves out because there the loan is already the context.
/// </summary>
public sealed record LoanPaymentListItemResponse(
    Guid Id,
    Guid LoanId,
    Guid BorrowerId,
    string BorrowerName,
    Guid GroupId,
    decimal Amount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    DateTime PaidDate,
    LoanPaymentType PaymentType,
    string? Notes,
    bool IsVerified,
    DateTime? VerifiedAt,
    DateTime CreatedAt);
