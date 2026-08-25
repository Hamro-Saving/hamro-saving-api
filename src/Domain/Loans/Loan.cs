using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

public sealed class Loan : Entity
{
    /// <summary>Rounding slack, in currency units, when comparing a submitted amount to a computed one.</summary>
    private const decimal Tolerance = 0.01m;

    public Guid Id { get; private set; }
    public Guid BorrowerId { get; private set; }
    public string BorrowerType { get; private set; } = string.Empty;
    public Guid GroupId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal InterestRate { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public LoanStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid? DisbursedById { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // --- Ledger. Interest runs daily on the outstanding principal at InterestRate per 365 days.
    // It is only written down when a payment happens; between payments it is computed on the fly
    // from LastAccrualDate, so the stored figures are always a settled, auditable position.

    /// <summary>When the money reached the borrower — interest runs from here.</summary>
    public DateTime? DisbursedAt { get; private set; }

    /// <summary>Principal still owed.</summary>
    public decimal OutstandingPrincipal { get; private set; }

    /// <summary>Interest owed at <see cref="LastAccrualDate"/> that the borrower has not paid yet.</summary>
    public decimal UnpaidInterest { get; private set; }

    /// <summary>The date interest has been settled up to — disbursement, then each payment.</summary>
    public DateTime? LastAccrualDate { get; private set; }

    public decimal TotalInterestAccrued { get; private set; }
    public decimal TotalInterestPaid { get; private set; }
    public decimal TotalPrincipalPaid { get; private set; }

    /// <summary>Interest earned per day at the current outstanding principal. Deliberately unrounded.</summary>
    public decimal DailyInterest => OutstandingPrincipal * InterestRate / 100m / 365m;

    private Loan() { }

    public static Loan Create(
        Guid borrowerId,
        string borrowerType,
        Guid groupId,
        decimal amount,
        decimal interestRate,
        DateTime startDate,
        DateTime? dueDate,
        string? notes)
    {
        var loan = new Loan
        {
            Id = Guid.CreateVersion7(),
            BorrowerId = borrowerId,
            BorrowerType = borrowerType,
            GroupId = groupId,
            Amount = amount,
            InterestRate = interestRate,
            StartDate = startDate,
            DueDate = dueDate,
            Status = LoanStatus.Pending,
            Notes = notes,
            DisbursedById = null,
            CreatedAt = DateTime.UtcNow
        };
        loan.Raise(new LoanCreatedDomainEvent(loan.Id, loan.BorrowerId, loan.GroupId));
        return loan;
    }

    /// <summary>Whole days of interest that have run since the last settled position.</summary>
    public int AccrualDays(DateTime asOf) =>
        LastAccrualDate is null ? 0 : Math.Max(0, (asOf.Date - LastAccrualDate.Value.Date).Days);

    /// <summary>
    /// Interest owed as of a date: what was left unpaid at the last transaction, plus what has
    /// run since. Only a live loan keeps accruing.
    /// </summary>
    public decimal InterestAccruedAsOf(DateTime asOf) =>
        Status is LoanStatus.Active or LoanStatus.Overdue
            ? UnpaidInterest + Round(DailyInterest * AccrualDays(asOf))
            : UnpaidInterest;

    /// <summary>What it would take to clear the loan on a given date.</summary>
    public decimal PayoffAmountAsOf(DateTime asOf) => OutstandingPrincipal + InterestAccruedAsOf(asOf);

    public void MarkAsOverdue() => Status = LoanStatus.Overdue;

    /// <summary>Admins may pull a loan any time before the money leaves the group.</summary>
    public Result Cancel()
    {
        if (Status is not (LoanStatus.Pending or LoanStatus.Approved))
            return Result.Failure(LoanErrors.CannotCancelAfterDisbursement);

        Status = LoanStatus.Cancelled;
        return Result.Success();
    }

    public Result ApproveLoan()
    {
        if (Status != LoanStatus.Pending) return Result.Failure(LoanErrors.NotPending);
        Status = LoanStatus.Approved;
        return Result.Success();
    }

    public Result Decline()
    {
        if (Status != LoanStatus.Pending) return Result.Failure(LoanErrors.NotPending);
        Status = LoanStatus.Declined;
        return Result.Success();
    }

    /// <summary>Money is with the borrower — the loan starts running and interest starts here.</summary>
    public Result CompleteDisbursement(Guid disbursedById, DateTime disbursedAt)
    {
        if (Status != LoanStatus.Approved) return Result.Failure(LoanErrors.NotApproved);

        Status = LoanStatus.Active;
        DisbursedById = disbursedById;
        DisbursedAt = disbursedAt;
        OutstandingPrincipal = Amount;
        UnpaidInterest = 0;
        LastAccrualDate = disbursedAt;
        Raise(new LoanCreatedDomainEvent(Id, BorrowerId, GroupId));
        return Result.Success();
    }

    /// <summary>
    /// Settles interest up to <paramref name="paidDate"/>, applies the payment, and moves the
    /// accrual anchor to that date so the next stretch of interest starts from a clean position.
    /// </summary>
    public Result<LoanPaymentAllocation> RecordPayment(DateTime paidDate, decimal principalAmount, decimal interestAmount)
    {
        if (Status is not (LoanStatus.Active or LoanStatus.Overdue) || LastAccrualDate is null)
            return Result.Failure<LoanPaymentAllocation>(LoanErrors.NotActive);

        if (paidDate.Date < LastAccrualDate.Value.Date)
            return Result.Failure<LoanPaymentAllocation>(LoanErrors.PaymentBeforeLastTransaction);

        var days = AccrualDays(paidDate);
        var newlyAccrued = Round(DailyInterest * days);
        var interestOwed = UnpaidInterest + newlyAccrued;

        if (interestAmount > interestOwed + Tolerance)
            return Result.Failure<LoanPaymentAllocation>(LoanErrors.InterestExceedsAccrued);

        if (principalAmount > OutstandingPrincipal + Tolerance)
            return Result.Failure<LoanPaymentAllocation>(LoanErrors.PrincipalExceedsOutstanding);

        // Within tolerance the submitted figures are treated as "pay it all off"
        var interestPaid = Math.Min(interestAmount, interestOwed);
        var principalPaid = Math.Min(principalAmount, OutstandingPrincipal);

        TotalInterestAccrued += newlyAccrued;
        UnpaidInterest = interestOwed - interestPaid;
        OutstandingPrincipal -= principalPaid;
        TotalInterestPaid += interestPaid;
        TotalPrincipalPaid += principalPaid;
        LastAccrualDate = paidDate;

        if (OutstandingPrincipal <= Tolerance && UnpaidInterest <= Tolerance)
        {
            OutstandingPrincipal = 0;
            UnpaidInterest = 0;
            Status = LoanStatus.PaidOff;
        }

        return Result.Success(new LoanPaymentAllocation(
            interestOwed,
            days,
            interestPaid,
            principalPaid,
            OutstandingPrincipal,
            UnpaidInterest));
    }

    /// <summary>
    /// Revises the loan before it is disbursed, by the borrower or an admin.
    ///
    /// Any revision returns it to Pending: a vote was cast on the loan as it stood, and
    /// once it has been changed there is no way to know whether that voter would still
    /// agree. The caller clears the votes to match.
    /// </summary>
    public Result Revise(decimal amount, decimal interestRate, DateTime? dueDate, string? notes)
    {
        if (Status is not (LoanStatus.Pending or LoanStatus.Approved))
            return Result.Failure(LoanErrors.CannotModifyAfterDisbursement);

        Amount = amount;
        InterestRate = interestRate;
        DueDate = dueDate;
        Notes = notes;
        Status = LoanStatus.Pending;

        return Result.Success();
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
