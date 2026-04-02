using HamroSavings.Domain.Loans;

namespace HamroSavings.Application.Loans.GetLoans;

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
    DateTime StartDate,
    DateTime? DueDate,
    LoanStatus Status,
    string? Notes,
    Guid? ApprovedById,
    DateTime CreatedAt);
