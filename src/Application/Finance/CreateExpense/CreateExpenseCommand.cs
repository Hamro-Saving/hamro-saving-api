using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.CreateExpense;

public sealed record CreateExpenseCommand(
    Guid GroupId,
    decimal Amount,
    string Category,
    string Description,
    DateTime ExpenseDate) : ICommand<Guid>;
