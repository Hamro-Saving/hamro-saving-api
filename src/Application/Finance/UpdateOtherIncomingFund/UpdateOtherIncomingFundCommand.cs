using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.UpdateOtherIncomingFund;

public sealed record UpdateOtherIncomingFundCommand(
    Guid RecordId,
    decimal Amount,
    DateTime PaidDate,
    string Remarks) : ICommand;
