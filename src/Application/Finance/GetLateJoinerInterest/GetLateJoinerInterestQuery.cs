using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.GetLateJoinerInterest;

public sealed record GetLateJoinerInterestQuery(Guid? GroupId = null) : IQuery<List<LateJoinerInterestResponse>>;

public sealed record LateJoinerInterestResponse(
    Guid Id,
    Guid MemberId,
    string MemberName,
    decimal Amount,
    DateTime PaidDate,
    string? Notes,
    DateTime CreatedAt);
