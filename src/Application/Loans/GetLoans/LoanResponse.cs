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
    int RequiredDeclines,
    bool HasCurrentUserApproved,
    bool HasCurrentUserDeclined,
    List<ApproverInfo> Approvers,
    List<ApproverInfo> Decliners,
    DateTime CreatedAt)
{
    /// <summary>
    /// The same loan with the group's governance stripped out: who voted, how many votes it
    /// takes, and which admin disbursed it. A non-member borrows from the group without
    /// joining it, so they see the terms of their own loan and nothing about the group
    /// deciding it — the approver names alone would undo the roster being private to them.
    /// </summary>
    public LoanResponse WithoutGroupInternals() => this with
    {
        DisbursedById = null,
        ApprovalCount = 0,
        DeclineCount = 0,
        RequiredApprovals = 0,
        RequiredDeclines = 0,
        HasCurrentUserApproved = false,
        HasCurrentUserDeclined = false,
        Approvers = [],
        Decliners = []
    };
}
