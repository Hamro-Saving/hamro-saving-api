using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Savings.DeleteDeposit;

public sealed record DeleteDepositCommand(Guid DepositId) : ICommand;
