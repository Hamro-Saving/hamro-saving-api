using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Savings;

public sealed class Deposit : Entity
{
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid GroupId { get; private set; }
    public decimal Amount { get; private set; }
    /// <summary>The Bikram Sambat month this covers. Only a monthly deposit has one.</summary>
    public int? DepositMonth { get; private set; }
    public int? DepositYear { get; private set; }
    public DateOnly DepositDate { get; private set; }
    public DepositType Type { get; private set; }
    public string? Notes { get; private set; }
    public bool IsVerified { get; private set; }
    public Guid? VerifiedById { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public Guid CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Deposit() { }

    public static Deposit Create(
        Guid memberId,
        Guid groupId,
        decimal amount,
        int? month,
        int? year,
        DateOnly depositDate,
        DepositType type,
        string? notes,
        Guid createdById)
    {
        var deposit = new Deposit
        {
            Id = Guid.CreateVersion7(),
            MemberId = memberId,
            GroupId = groupId,
            Amount = amount,
            // A period belongs to a monthly deposit and nothing else.
            DepositMonth = type == DepositType.MonthlyDeposit ? month : null,
            DepositYear = type == DepositType.MonthlyDeposit ? year : null,
            DepositDate = depositDate,
            Type = type,
            Notes = notes,
            IsVerified = false,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
        deposit.Raise(new DepositRecordedDomainEvent(deposit.Id, deposit.MemberId, deposit.GroupId));
        return deposit;
    }

    public Result Verify(Guid verifiedById)
    {
        if (IsVerified) return Result.Failure(DepositErrors.AlreadyVerified);
        IsVerified = true;
        VerifiedById = verifiedById;
        VerifiedAt = DateTime.UtcNow;
        Raise(new DepositVerifiedDomainEvent(Id, MemberId, GroupId));
        return Result.Success();
    }

    public Result Update(decimal amount, string? notes, DateOnly depositDate)
    {
        if (IsVerified) return Result.Failure(DepositErrors.CannotModifyVerified);
        if (depositDate > DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Failure(DepositErrors.DepositDateInFuture);

        Amount = amount;
        Notes = notes;
        DepositDate = depositDate;
        return Result.Success();
    }
}
