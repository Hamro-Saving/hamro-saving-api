using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.VerifyOtherIncomingFund;

public sealed record VerifyOtherIncomingFundCommand(Guid RecordId) : ICommand;
