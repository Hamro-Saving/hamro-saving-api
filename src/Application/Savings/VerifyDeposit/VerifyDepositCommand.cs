using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Savings.VerifyDeposit;

public sealed record VerifyDepositCommand(Guid DepositId) : ICommand;
