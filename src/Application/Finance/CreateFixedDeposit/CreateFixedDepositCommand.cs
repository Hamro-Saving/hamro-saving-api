using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.CreateFixedDeposit;

public sealed record CreateFixedDepositCommand(
    Guid GroupId,
    string InstitutionName,
    decimal Amount,
    decimal InterestRate,
    DateTime StartDate,
    DateTime MaturityDate,
    string? Notes) : ICommand<Guid>;
