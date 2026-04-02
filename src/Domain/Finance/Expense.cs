using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Finance;

public sealed class Expense : Entity
{
    public Guid Id { get; private set; }
    public Guid GroupId { get; private set; }
    public decimal Amount { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime ExpenseDate { get; private set; }
    public Guid? ApprovedById { get; private set; }
    public Guid CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Expense() { }

    public static Expense Create(
        Guid groupId,
        decimal amount,
        string category,
        string description,
        DateTime expenseDate,
        Guid createdById)
    {
        return new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Amount = amount,
            Category = category,
            Description = description,
            ExpenseDate = expenseDate,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(Guid approvedById) => ApprovedById = approvedById;

    public void Update(decimal amount, string category, string description, DateTime expenseDate)
    {
        Amount = amount;
        Category = category;
        Description = description;
        ExpenseDate = expenseDate;
    }
}
