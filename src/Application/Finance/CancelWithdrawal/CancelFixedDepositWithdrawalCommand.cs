using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.CancelWithdrawal;

public sealed record CancelFixedDepositWithdrawalCommand(Guid FixedDepositId) : ICommand;
