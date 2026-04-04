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
        string? notes,
        Guid? approvedById)
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
            Status = LoanStatus.Active,
            Notes = notes,
            ApprovedById = approvedById,
            CreatedAt = DateTime.UtcNow
        };
        loan.Raise(new LoanCreatedDomainEvent(loan.Id, loan.BorrowerId, loan.GroupId));
        return loan;
    }

    public void MarkAsPaidOff() => Status = LoanStatus.PaidOff;
    public void MarkAsOverdue() => Status = LoanStatus.Overdue;
    public void Cancel() => Status = LoanStatus.Cancelled;
    public void Approve(Guid approvedById) => ApprovedById = approvedById;
}
