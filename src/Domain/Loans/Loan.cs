using HamroSavings.Domain.Ledger;
using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

public sealed class Loan : Entity
{
    /// <summary>Rounding slack, in currency units, when comparing a submitted amount to a computed one.</summary>
    private const decimal Tolerance = 0.01m;

    /// <summary>
    /// What is left of a loan before it counts as settled. Chasing a borrower for the last few
    /// paisa of a daily-interest remainder is not worth anyone's time, so anything under a
    /// rupee closes the loan and is written off rather than carried.
    /// </summary>
    private const decimal SettlementThreshold = 1m;

    public Guid Id { get; private set; }
    public Guid BorrowerId { get; private set; }
    public string BorrowerType { get; private set; } = string.Empty;
    public Guid GroupId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal InterestRate { get; private set; }
    /// <summary>
    /// What the members actually carried. <see cref="Amount"/> is what the loan is now, which
    /// a short disbursement rewrites; this keeps the figure that was asked for and voted on,
    /// so a loan paid out below its request still shows what the group agreed to.
    /// </summary>
    public decimal RequestedAmount { get; private set; }

    /// <summary>Whether less was handed over than the members approved.</summary>
    public bool WasReducedAtDisbursement => Amount < RequestedAmount;

    public DateTime StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public LoanStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid? DisbursedById { get; private set; }

    /// <summary>
    /// Whether an admin paid this out without the members' approval. A forced loan spends the
    /// group's money on one person's say-so, so it stays marked as such for as long as it exists.
    /// </summary>
    public bool IsForceDisbursed { get; private set; }

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
            RequestedAmount = amount,
            InterestRate = interestRate,
            StartDate = startDate,
            DueDate = dueDate,
            Status = LoanStatus.Pending,
            Notes = notes,
            DisbursedById = null,
            CreatedAt = DateTime.UtcNow
        };
        loan.Raise(new LoanRequestedDomainEvent(loan.Id, loan.GroupId, loan.BorrowerId));
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
        Raise(new LoanVoteSettledDomainEvent(Id, GroupId, BorrowerId, IsApproved: true));
        return Result.Success();
    }

    public Result Decline()
    {
        if (Status != LoanStatus.Pending) return Result.Failure(LoanErrors.NotPending);
        Status = LoanStatus.Declined;
        Raise(new LoanVoteSettledDomainEvent(Id, GroupId, BorrowerId, IsApproved: false));
        return Result.Success();
    }

    /// <summary>Money is with the borrower — the loan starts running and interest starts here.</summary>
    /// <param name="disbursedAmount">
    /// What actually left the group. Null means the whole figure asked for, which is the
    /// ordinary case. Less is allowed and rewrites the loan; more is not.
    /// </param>
    public Result CompleteDisbursement(Guid disbursedById, DateTime disbursedAt, CashInHand available, decimal? disbursedAmount = null)
    {
        if (Status != LoanStatus.Approved) return Result.Failure(LoanErrors.NotApproved);

        var paidOut = disbursedAmount ?? Amount;

        if (paidOut <= 0)
            return Result.Failure(LoanErrors.DisbursedAmountNotPositive);

        // Handing over less than was asked is the group's call to make — a short payout simply
        // becomes the loan. Handing over more is not: the members carried a figure, and anything
        // above it was never agreed to by anyone.
        if (paidOut > Amount + Tolerance)
            return Result.Failure(LoanErrors.DisbursedAmountExceedsRequest(paidOut, Amount));

        // The payout can be backdated — a loan the group made before it kept records here is
        // entered after the fact — but it cannot be dated forward: interest would run from a
        // day the borrower had not been handed anything.
        if (disbursedAt.Date > DateTime.UtcNow.Date)
            return Result.Failure(LoanErrors.DisbursementInFuture);

        // This is the moment the money actually leaves, so it is the moment that matters:
        // the group may have had it when the loan was approved and not when it is paid out.
        var covered = available.EnsureCovers(paidOut);
        if (covered.IsFailure) return covered;

        // What left the group is what is owed back, so a short payout is the loan from here on.
        // RequestedAmount deliberately stays where it was: the record should still show what
        // the members agreed to, not quietly restate it as the smaller figure.
        Amount = paidOut;
        Status = LoanStatus.Active;
        DisbursedById = disbursedById;
        DisbursedAt = disbursedAt;
        OutstandingPrincipal = Amount;
        UnpaidInterest = 0;
        LastAccrualDate = disbursedAt;
        Raise(new LoanDisbursedDomainEvent(Id, GroupId, BorrowerId));
        return Result.Success();
    }

    /// <summary>
    /// Pays out a loan the members never settled, on an admin's authority. This exists because a
    /// vote that nobody answers leaves a borrower waiting indefinitely; it is not a way past a
    /// vote that was answered. A group that has refused the loan keeps its refusal, and the
    /// cash rule binds an admin exactly as it binds anyone else.
    /// </summary>
    public Result ForceDisbursement(Guid disbursedById, DateTime disbursedAt, CashInHand available, LoanVoteTally votes, decimal? disbursedAmount = null)
    {
        if (Status is not (LoanStatus.Pending or LoanStatus.Approved))
            return Result.Failure(LoanErrors.CannotForceDisburse);

        if (votes.GroupHasRefused)
            return Result.Failure(LoanErrors.GroupRefusedLoan);

        if (disbursedAt.Date > DateTime.UtcNow.Date)
            return Result.Failure(LoanErrors.DisbursementInFuture);

        var covered = available.EnsureCovers(disbursedAmount ?? Amount);
        if (covered.IsFailure) return covered;

        IsForceDisbursed = Status == LoanStatus.Pending;
        Status = LoanStatus.Approved;
        return CompleteDisbursement(disbursedById, disbursedAt, available, disbursedAmount);
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

        if (principalAmount > OutstandingPrincipal + Tolerance)
            return Result.Failure<LoanPaymentAllocation>(LoanErrors.PrincipalExceedsOutstanding);

        // Interest is recorded as handed over, even above what has accrued: a borrower settling
        // on a round figure, or on the rate the group agreed by hand, is paying interest and the
        // books should say so. Principal is different — there is a fixed debt to clear, and more
        // than that is not a repayment at all.
        var interestPaid = interestAmount;
        var principalPaid = Math.Min(principalAmount, OutstandingPrincipal);

        TotalInterestAccrued += newlyAccrued;
        // Never below zero: paying ahead settles what is owed, it does not build a credit the
        // loan would have to remember and net off against interest not yet earned.
        UnpaidInterest = Math.Max(0, interestOwed - interestPaid);
        OutstandingPrincipal -= principalPaid;
        TotalInterestPaid += interestPaid;
        TotalPrincipalPaid += principalPaid;
        LastAccrualDate = paidDate;

        if (OutstandingPrincipal + UnpaidInterest < SettlementThreshold)
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
    public Result Revise(decimal amount, decimal interestRate, DateTime startDate, DateTime? dueDate, string? notes)
    {
        if (Status is not (LoanStatus.Pending or LoanStatus.Approved))
            return Result.Failure(LoanErrors.CannotModifyAfterDisbursement);

        Amount = amount;
        // A revision is a new request, put back to the group — so this moves with it. Only a
        // short disbursement parts the two.
        RequestedAmount = amount;
        InterestRate = interestRate;
        // Safe to move: interest runs from DisbursedAt, and a revision is only possible
        // before the money leaves the group, so nothing has accrued against this date yet.
        StartDate = startDate;
        DueDate = dueDate;
        Notes = notes;
        Status = LoanStatus.Pending;
        Raise(new LoanRequestedDomainEvent(Id, GroupId, BorrowerId));

        return Result.Success();
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
