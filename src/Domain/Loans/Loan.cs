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
    public Guid? DisbursedById { get; private set; }
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
            DisbursedById = null,
            CreatedAt = DateTime.UtcNow
        };
        loan.Raise(new LoanCreatedDomainEvent(loan.Id, loan.BorrowerId, loan.GroupId));
        return loan;
    }

    public void MarkAsPaidOff() => Status = LoanStatus.PaidOff;
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
        return Result.Success();
    }

    public Result Decline()
    {
        if (Status != LoanStatus.Pending) return Result.Failure(LoanErrors.NotPending);
        Status = LoanStatus.Declined;
        return Result.Success();
    }

    /// <summary>Money is with the borrower — the loan starts running.</summary>
    public Result CompleteDisbursement(Guid disbursedById)
    {
        if (Status != LoanStatus.Approved) return Result.Failure(LoanErrors.NotApproved);
        Status = LoanStatus.Active;
        DisbursedById = disbursedById;
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
