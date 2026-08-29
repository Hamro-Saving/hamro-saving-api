using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.UpdateFixedDeposit;

public sealed record UpdateFixedDepositCommand(
    Guid FixedDepositId,
    string InstitutionName,
    decimal Amount,
    decimal InterestRate,
    DateTime StartDate,
    DateTime MaturityDate,
    string? Notes) : ICommand;
