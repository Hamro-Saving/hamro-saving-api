using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Savings;

namespace HamroSavings.Application.Savings.CreateDeposit;

public sealed record CreateDepositCommand(
    Guid MemberId,
    Guid GroupId,
    decimal Amount,
    int DepositMonth,
    int DepositYear,
    DepositType Type,
    string? Notes) : ICommand<Guid>;
