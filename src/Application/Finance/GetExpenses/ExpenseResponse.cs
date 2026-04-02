namespace HamroSavings.Application.Finance.GetExpenses;

public sealed record ExpenseResponse(
    Guid Id,
    Guid GroupId,
    decimal Amount,
    string Category,
    string Description,
    DateTime ExpenseDate,
    Guid? ApprovedById,
    Guid CreatedById,
    DateTime CreatedAt);
