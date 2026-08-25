using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.RecordLateJoinerInterest;

public sealed record RecordLateJoinerInterestCommand(
    Guid MemberId,
    decimal Amount,
    DateTime PaidDate,
    string? Notes,
    Guid? GroupId = null) : ICommand<Guid>;
