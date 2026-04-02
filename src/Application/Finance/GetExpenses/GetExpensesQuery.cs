using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.GetExpenses;

public sealed record GetExpensesQuery(
    Guid? GroupId = null,
    string? Category = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IQuery<List<ExpenseResponse>>;
