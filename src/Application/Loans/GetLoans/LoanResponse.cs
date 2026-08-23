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
    // Live ledger: principal still owed, interest run to today, and what would clear it now
    decimal OutstandingPrincipal,
    decimal AccruedInterest,
    decimal PayoffAmount,
    decimal DailyInterest,
    decimal UnpaidInterest,
    decimal TotalPrincipalPaid,
    decimal TotalInterestPaid,
    DateTime? DisbursedAt,
    DateTime? LastAccrualDate,
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
