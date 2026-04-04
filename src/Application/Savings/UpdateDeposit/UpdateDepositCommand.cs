using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Savings.UpdateDeposit;

public sealed record UpdateDepositCommand(Guid DepositId, decimal Amount, string? Notes) : ICommand;
