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

    // --- The interest calculation this payment was settled against, frozen at that moment.

    /// <summary>Interest the loan owed just before this payment was applied.</summary>
    public decimal InterestOwedBefore { get; private set; }

    /// <summary>Days of interest that had run since the loan's previous transaction.</summary>
    public int DaysAccrued { get; private set; }

    public decimal OutstandingPrincipalAfter { get; private set; }
    public decimal UnpaidInterestAfter { get; private set; }

    private LoanPayment() { }

    public static LoanPayment Create(
        Guid loanId,
        DateTime paidDate,
        LoanPaymentAllocation allocation,
        string? notes,
        Guid createdById)
    {
        var payment = new LoanPayment
        {
            Id = Guid.CreateVersion7(),
            LoanId = loanId,
            Amount = allocation.PrincipalPaid + allocation.InterestPaid,
            PrincipalAmount = allocation.PrincipalPaid,
            InterestAmount = allocation.InterestPaid,
            PaidDate = paidDate,
            PaymentType = TypeOf(allocation),
            Notes = notes,
            IsVerified = false,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
            InterestOwedBefore = allocation.InterestOwedBefore,
            DaysAccrued = allocation.DaysAccrued,
            OutstandingPrincipalAfter = allocation.OutstandingPrincipalAfter,
            UnpaidInterestAfter = allocation.UnpaidInterestAfter
        };
        payment.Raise(new LoanPaymentRecordedDomainEvent(payment.Id, payment.LoanId));
        return payment;
    }

    private static LoanPaymentType TypeOf(LoanPaymentAllocation allocation) =>
        allocation.PrincipalPaid > 0 && allocation.InterestPaid > 0 ? LoanPaymentType.Mixed
        : allocation.InterestPaid > 0 ? LoanPaymentType.Interest
        : LoanPaymentType.Principal;

    /// <summary>
    /// Corrects what the payer is recorded as having handed over. Only while unverified: once
    /// verified the money is in the group's books, and a ledger entry is corrected by an
    /// opposite entry rather than by rewriting what it refers to.
    ///
    /// The derived figures are deliberately left alone — the loan settles them when the
    /// payment is applied again, through <see cref="Restate"/>.
    /// </summary>
    public Result Revise(DateTime paidDate, decimal principalAmount, decimal interestAmount, string? notes)
    {
        if (IsVerified) return Result.Failure(LoanErrors.CannotModifyVerifiedPayment);

        PaidDate = paidDate;
        PrincipalAmount = principalAmount;
        InterestAmount = interestAmount;
        Notes = notes;
        return Result.Success();
    }

    /// <summary>
    /// Writes back what the loan made of this payment once it has been applied again. Called
    /// for every payment of a replay, including ones already verified: their position is
    /// recomputed from the same inputs, so it lands exactly where it was.
    /// </summary>
    public void Restate(LoanPaymentAllocation allocation)
    {
        Amount = allocation.PrincipalPaid + allocation.InterestPaid;
        PrincipalAmount = allocation.PrincipalPaid;
        InterestAmount = allocation.InterestPaid;
        PaymentType = TypeOf(allocation);
        InterestOwedBefore = allocation.InterestOwedBefore;
        DaysAccrued = allocation.DaysAccrued;
        OutstandingPrincipalAfter = allocation.OutstandingPrincipalAfter;
        UnpaidInterestAfter = allocation.UnpaidInterestAfter;
    }

    public Result Verify(Guid verifiedById)
    {
        if (IsVerified) return Result.Failure(LoanErrors.PaymentAlreadyVerified);
        IsVerified = true;
        VerifiedById = verifiedById;
        VerifiedAt = DateTime.UtcNow;
        Raise(new LoanPaymentVerifiedDomainEvent(Id, LoanId));
        return Result.Success();
    }
}
