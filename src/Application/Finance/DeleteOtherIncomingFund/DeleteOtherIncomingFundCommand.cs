using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.DeleteOtherIncomingFund;

public sealed record DeleteOtherIncomingFundCommand(Guid RecordId) : ICommand;
