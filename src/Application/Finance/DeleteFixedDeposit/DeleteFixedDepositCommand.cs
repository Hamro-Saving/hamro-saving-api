using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.DeleteFixedDeposit;

public sealed record DeleteFixedDepositCommand(Guid FixedDepositId) : ICommand;
