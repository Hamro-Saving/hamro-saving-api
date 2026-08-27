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
    public bool IsVerified { get; private set; }
    public Guid? VerifiedById { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
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
        var expense = new Expense
        {
            Id = Guid.CreateVersion7(),
            GroupId = groupId,
            Amount = amount,
            Category = category,
            Description = description,
            ExpenseDate = expenseDate,
            IsVerified = false,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
        expense.Raise(new ExpenseRecordedDomainEvent(expense.Id, expense.GroupId));
        return expense;
    }

    public Result Verify(Guid verifiedById)
    {
        if (IsVerified) return Result.Failure(ExpenseErrors.AlreadyVerified);
        IsVerified = true;
        VerifiedById = verifiedById;
        VerifiedAt = DateTime.UtcNow;
        Raise(new ExpenseVerifiedDomainEvent(Id, GroupId));
        return Result.Success();
    }

    public Result Update(decimal amount, string category, string description, DateTime expenseDate)
    {
        if (IsVerified) return Result.Failure(ExpenseErrors.CannotModifyVerified);

        Amount = amount;
        Category = category;
        Description = description;
        ExpenseDate = expenseDate;
        return Result.Success();
    }
}
