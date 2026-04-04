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

    public decimal ExpectedMaturityAmount => Amount + (Amount * InterestRate / 100);

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
    public void MarkAsWithdrawn() => Status = FixedDepositStatus.Withdrawn;
}
