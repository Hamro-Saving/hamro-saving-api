using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.VerifyExpense;

public sealed record VerifyExpenseCommand(Guid ExpenseId) : ICommand;
