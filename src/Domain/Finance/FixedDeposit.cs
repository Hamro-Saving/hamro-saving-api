using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Finance;

public sealed class FixedDeposit : Entity
{
    public Guid Id { get; private set; }
    public Guid GroupId { get; private set; }
    public string InstitutionName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal InterestRate { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime MaturityDate { get; private set; }
    public FixedDepositStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public bool IsVerified { get; private set; }
    public Guid? VerifiedById { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public Guid CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Interest the institution actually paid out, which need not match the expected figure.</summary>
    public decimal? InterestEarned { get; private set; }

    public DateTime? WithdrawnAt { get; private set; }
    public Guid? WithdrawnById { get; private set; }

    /// <summary>
    /// Checked separately from the placement: a withdrawal is a second movement of money, and
    /// a placement approved months earlier says nothing about the interest figure.
    /// </summary>
    public bool IsWithdrawalVerified { get; private set; }
    public Guid? WithdrawalVerifiedById { get; private set; }
    public DateTime? WithdrawalVerifiedAt { get; private set; }

    public decimal ExpectedMaturityAmount => Amount + (Amount * InterestRate / 100);

    /// <summary>The maturity date has come around, whether or not anyone has recorded it.</summary>
    public bool HasMatured(DateTime asOf) => asOf.Date >= MaturityDate.Date;

    /// <summary>
    /// The status as the world actually sees it: a deposit stops being active the day it
    /// matures, even though nothing has been withdrawn yet.
    /// </summary>
    public FixedDepositStatus StatusAsOf(DateTime asOf) =>
        Status == FixedDepositStatus.Active && HasMatured(asOf)
            ? FixedDepositStatus.Matured
            : Status;

    private FixedDeposit() { }

    public static FixedDeposit Create(
        Guid groupId,
        string institutionName,
        decimal amount,
        decimal interestRate,
        DateTime startDate,
        DateTime maturityDate,
        string? notes,
        Guid createdById)
    {
        var fixedDeposit = new FixedDeposit
        {
            Id = Guid.CreateVersion7(),
            GroupId = groupId,
            InstitutionName = institutionName,
            Amount = amount,
            InterestRate = interestRate,
            StartDate = startDate,
            MaturityDate = maturityDate,
            Status = FixedDepositStatus.Active,
            Notes = notes,
            IsVerified = false,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
        fixedDeposit.Raise(new FixedDepositRecordedDomainEvent(fixedDeposit.Id, fixedDeposit.GroupId));
        return fixedDeposit;
    }

    public Result Verify(Guid verifiedById)
    {
        if (IsVerified) return Result.Failure(FixedDepositErrors.AlreadyVerified);
        IsVerified = true;
        VerifiedById = verifiedById;
        VerifiedAt = DateTime.UtcNow;
        Raise(new FixedDepositVerifiedDomainEvent(Id, GroupId));
        return Result.Success();
    }

    /// <summary>Kept apart from <see cref="Verify"/> so the two movements are answered for separately.</summary>
    public Result VerifyWithdrawal(Guid verifiedById)
    {
        if (Status != FixedDepositStatus.Withdrawn)
            return Result.Failure(FixedDepositErrors.NotWithdrawn);

        if (IsWithdrawalVerified) return Result.Failure(FixedDepositErrors.WithdrawalAlreadyVerified);

        IsWithdrawalVerified = true;
        WithdrawalVerifiedById = verifiedById;
        WithdrawalVerifiedAt = DateTime.UtcNow;
        Raise(new FixedDepositWithdrawalVerifiedDomainEvent(Id, GroupId));
        return Result.Success();
    }

    public void MarkAsMatured() => Status = FixedDepositStatus.Matured;

    /// <summary>
    /// The money is back with the group. The interest is whatever the institution actually
    /// paid — early withdrawals often return less than the expected amount.
    /// </summary>
    /// <summary>
    /// Corrects the placement before anyone has checked it. Once verified the money has left
    /// the group in its books, and that is corrected by an opposite entry rather than by
    /// rewriting the record it refers to.
    /// </summary>
    public Result Update(
        string institutionName,
        decimal amount,
        decimal interestRate,
        DateTime startDate,
        DateTime maturityDate,
        string? notes)
    {
        if (IsVerified) return Result.Failure(FixedDepositErrors.CannotModifyVerified);

        InstitutionName = institutionName;
        Amount = amount;
        InterestRate = interestRate;
        StartDate = startDate;
        MaturityDate = maturityDate;
        Notes = notes;
        return Result.Success();
    }

    /// <summary>
    /// Corrects a withdrawal that has been recorded but not yet checked — usually the interest
    /// figure, once someone reads what the institution actually paid.
    ///
    /// Separate from <see cref="Withdraw"/> on purpose: withdrawing happens once, and a second
    /// attempt at it is a mistake worth refusing. This says plainly that it is the same
    /// withdrawal being restated, so no notification goes out and nothing new happens.
    /// </summary>
    public Result ReviseWithdrawal(decimal interestEarned, DateTime withdrawnAt)
    {
        var editable = EnsureWithdrawalIsOpen();
        if (editable.IsFailure) return editable;

        if (interestEarned < 0)
            return Result.Failure(FixedDepositErrors.NegativeInterest);

        if (withdrawnAt.Date < StartDate.Date)
            return Result.Failure(FixedDepositErrors.WithdrawnBeforeStart);

        InterestEarned = interestEarned;
        WithdrawnAt = withdrawnAt;
        return Result.Success();
    }

    /// <summary>
    /// Takes back a withdrawal recorded in error, leaving the deposit placed as it was. Only
    /// while unverified — a verified withdrawal is money that has moved in the books.
    /// </summary>
    public Result CancelWithdrawal()
    {
        var editable = EnsureWithdrawalIsOpen();
        if (editable.IsFailure) return editable;

        // Back to placed. Whether that reads as matured is the maturity date's business,
        // which StatusAsOf works out, so nothing here needs to decide it.
        Status = FixedDepositStatus.Active;
        InterestEarned = null;
        WithdrawnAt = null;
        WithdrawnById = null;
        return Result.Success();
    }

    private Result EnsureWithdrawalIsOpen()
    {
        if (Status != FixedDepositStatus.Withdrawn)
            return Result.Failure(FixedDepositErrors.NotWithdrawn);

        if (IsWithdrawalVerified)
            return Result.Failure(FixedDepositErrors.WithdrawalAlreadyVerified);

        return Result.Success();
    }

    public Result Withdraw(decimal interestEarned, DateTime withdrawnAt, Guid withdrawnById)
    {
        if (Status == FixedDepositStatus.Withdrawn)
            return Result.Failure(FixedDepositErrors.AlreadyWithdrawn);

        // Otherwise the return posts against a placement the ledger has never heard of.
        if (!IsVerified)
            return Result.Failure(FixedDepositErrors.NotVerified);

        if (interestEarned < 0)
            return Result.Failure(FixedDepositErrors.NegativeInterest);

        if (withdrawnAt.Date < StartDate.Date)
            return Result.Failure(FixedDepositErrors.WithdrawnBeforeStart);

        Status = FixedDepositStatus.Withdrawn;
        InterestEarned = interestEarned;
        WithdrawnAt = withdrawnAt;
        WithdrawnById = withdrawnById;
        IsWithdrawalVerified = false;
        Raise(new FixedDepositWithdrawalRecordedDomainEvent(Id, GroupId));
        return Result.Success();
    }
}
