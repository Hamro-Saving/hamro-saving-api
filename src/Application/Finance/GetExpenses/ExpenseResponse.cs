namespace HamroSavings.Application.Finance.GetExpenses;

public sealed record ExpenseResponse(
    Guid Id,
    Guid GroupId,
    decimal Amount,
    string Category,
    string Description,
    DateTime ExpenseDate,
    bool IsVerified,
    Guid? VerifiedById,
    DateTime? VerifiedAt,
    Guid CreatedById,
    DateTime CreatedAt);
