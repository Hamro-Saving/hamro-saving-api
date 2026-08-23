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
    public Guid CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Interest the institution actually paid out, which need not match the expected figure.</summary>
    public decimal? InterestEarned { get; private set; }

    public DateTime? WithdrawnAt { get; private set; }
    public Guid? WithdrawnById { get; private set; }

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
        return new FixedDeposit
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
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsMatured() => Status = FixedDepositStatus.Matured;

    /// <summary>
    /// The money is back with the group. The interest is whatever the institution actually
    /// paid — early withdrawals often return less than the expected amount.
    /// </summary>
    public Result Withdraw(decimal interestEarned, DateTime withdrawnAt, Guid withdrawnById)
    {
        if (Status == FixedDepositStatus.Withdrawn)
            return Result.Failure(FixedDepositErrors.AlreadyWithdrawn);

        if (interestEarned < 0)
            return Result.Failure(FixedDepositErrors.NegativeInterest);

        if (withdrawnAt.Date < StartDate.Date)
            return Result.Failure(FixedDepositErrors.WithdrawnBeforeStart);

        Status = FixedDepositStatus.Withdrawn;
        InterestEarned = interestEarned;
        WithdrawnAt = withdrawnAt;
        WithdrawnById = withdrawnById;
        return Result.Success();
    }
}
