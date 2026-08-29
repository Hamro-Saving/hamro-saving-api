using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.UpdateExpense;

public sealed record UpdateExpenseCommand(
    Guid ExpenseId,
    decimal Amount,
    string Category,
    string Description,
    DateTime ExpenseDate) : ICommand;
