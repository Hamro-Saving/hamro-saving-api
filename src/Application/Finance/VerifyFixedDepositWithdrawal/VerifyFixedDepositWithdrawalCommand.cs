using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.VerifyFixedDepositWithdrawal;

public sealed record VerifyFixedDepositWithdrawalCommand(Guid FixedDepositId) : ICommand;
