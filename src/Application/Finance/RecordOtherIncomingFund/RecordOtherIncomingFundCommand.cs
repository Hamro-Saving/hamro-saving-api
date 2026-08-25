using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.RecordOtherIncomingFund;

public sealed record RecordOtherIncomingFundCommand(
    Guid MemberId,
    decimal Amount,
    DateTime PaidDate,
    string Remarks,
    Guid? GroupId = null) : ICommand<Guid>;
