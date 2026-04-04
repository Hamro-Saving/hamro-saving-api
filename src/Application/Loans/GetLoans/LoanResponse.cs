using HamroSavings.Domain.Loans;

namespace HamroSavings.Application.Loans.GetLoans;

public sealed record ApproverInfo(Guid ApproverId, string ApproverName, DateTime ApprovedAt);

public sealed record LoanResponse(
    Guid Id,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerType,
    Guid GroupId,
    decimal Amount,
    decimal InterestRate,
    decimal TotalInterest,
    decimal TotalDue,
    decimal AccruedInterest,
    DateTime StartDate,
    DateTime? DueDate,
    LoanStatus Status,
    string? Notes,
    Guid? ApprovedById,
    int ApprovalCount,
    int RequiredApprovals,
    bool HasCurrentUserApproved,
    List<ApproverInfo> Approvers,
    DateTime CreatedAt);
