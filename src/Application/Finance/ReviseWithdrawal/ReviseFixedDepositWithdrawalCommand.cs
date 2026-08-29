using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.ReviseWithdrawal;

/// <summary>
/// Restates a withdrawal that has been recorded but not verified — usually the interest
/// figure, once someone reads what the institution actually paid.
/// </summary>
public sealed record ReviseFixedDepositWithdrawalCommand(
    Guid FixedDepositId,
    decimal InterestEarned,
    DateTime WithdrawnAt) : ICommand;
