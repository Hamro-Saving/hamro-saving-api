using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

public sealed class LoanPayment : Entity
{
    public Guid Id { get; private set; }
    public Guid LoanId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal PrincipalAmount { get; private set; }
    public decimal InterestAmount { get; private set; }
    public DateTime PaidDate { get; private set; }
    public LoanPaymentType PaymentType { get; private set; }
    public string? Notes { get; private set; }
    public bool IsVerified { get; private set; }
    public Guid? VerifiedById { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public Guid CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private LoanPayment() { }

    public static LoanPayment Create(
        Guid loanId,
        decimal amount,
        decimal principalAmount,
        decimal interestAmount,
        DateTime paidDate,
        LoanPaymentType paymentType,
        string? notes,
        Guid createdById)
    {
        return new LoanPayment
        {
            Id = Guid.CreateVersion7(),
            LoanId = loanId,
            Amount = amount,
            PrincipalAmount = principalAmount,
            InterestAmount = interestAmount,
            PaidDate = paidDate,
            PaymentType = paymentType,
            Notes = notes,
            IsVerified = false,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result Verify(Guid verifiedById)
    {
        if (IsVerified) return Result.Failure(LoanErrors.PaymentAlreadyVerified);
        IsVerified = true;
        VerifiedById = verifiedById;
        VerifiedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
