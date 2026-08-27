using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.VerifyFixedDeposit;

public sealed record VerifyFixedDepositCommand(Guid FixedDepositId) : ICommand;
