using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.DeleteExpense;

public sealed record DeleteExpenseCommand(Guid ExpenseId) : ICommand;
