using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

public sealed class Loan : Entity
{
    public Guid Id { get; private set; }
    public Guid BorrowerId { get; private set; }
    public string BorrowerType { get; private set; } = string.Empty;
    public Guid GroupId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal InterestRate { get; private set; }
    public decimal TotalInterest => Amount * InterestRate / 100;
    public decimal TotalDue => Amount + TotalInterest;
    public DateTime StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public LoanStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid? ApprovedById { get; private set; }
    public DateTime CreatedAt { get; private set; }

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
            ApprovedById = null,
            CreatedAt = DateTime.UtcNow
        };
        loan.Raise(new LoanCreatedDomainEvent(loan.Id, loan.BorrowerId, loan.GroupId));
        return loan;
    }

    public void MarkAsPaidOff() => Status = LoanStatus.PaidOff;
    public void MarkAsOverdue() => Status = LoanStatus.Overdue;
    public void Cancel() => Status = LoanStatus.Cancelled;

    public Result ApproveLoan()
    {
        if (Status != LoanStatus.Pending) return Result.Failure(LoanErrors.NotPending);
        Status = LoanStatus.Approved;
        return Result.Success();
    }

    public Result Verify(Guid verifiedById)
    {
        if (Status != LoanStatus.Approved) return Result.Failure(LoanErrors.NotApproved);
        Status = LoanStatus.Active;
        ApprovedById = verifiedById;
        Raise(new LoanCreatedDomainEvent(Id, BorrowerId, GroupId));
        return Result.Success();
    }

    public Result Update(decimal amount, decimal interestRate, DateTime? dueDate, string? notes)
    {
        if (Status != LoanStatus.Pending) return Result.Failure(LoanErrors.CannotModifyApproved);
        Amount = amount;
        InterestRate = interestRate;
        DueDate = dueDate;
        Notes = notes;
        return Result.Success();
    }
}
