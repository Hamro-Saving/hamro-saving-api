using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Savings;

public sealed class Deposit : Entity
{
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid GroupId { get; private set; }
    public decimal Amount { get; private set; }
    public int DepositMonth { get; private set; }
    public int DepositYear { get; private set; }
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
        int month,
        int year,
        DepositType type,
        string? notes,
        Guid createdById)
    {
        return new Deposit
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            GroupId = groupId,
            Amount = amount,
            DepositMonth = month,
            DepositYear = year,
            Type = type,
            Notes = notes,
            IsVerified = false,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
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

    public Result Update(decimal amount, string? notes)
    {
        if (IsVerified) return Result.Failure(DepositErrors.CannotModifyVerified);
        Amount = amount;
        Notes = notes;
        return Result.Success();
    }
}
