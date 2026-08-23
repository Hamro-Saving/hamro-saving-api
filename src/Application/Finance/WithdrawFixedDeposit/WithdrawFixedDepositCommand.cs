using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.WithdrawFixedDeposit;

/// <summary>
/// Closes a fixed deposit. <paramref name="InterestEarned"/> is what the institution actually
/// paid out, which an early withdrawal or a revised rate can make differ from the expected figure.
/// </summary>
public sealed record WithdrawFixedDepositCommand(
    Guid FixedDepositId,
    decimal InterestEarned,
    DateTime WithdrawnAt) : ICommand;
