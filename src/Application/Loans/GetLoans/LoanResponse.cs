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
    Guid? DisbursedById,
    int ApprovalCount,
    int DeclineCount,
    int RequiredApprovals,
    bool HasCurrentUserApproved,
    bool HasCurrentUserDeclined,
    List<ApproverInfo> Approvers,
    List<ApproverInfo> Decliners,
    DateTime CreatedAt);
