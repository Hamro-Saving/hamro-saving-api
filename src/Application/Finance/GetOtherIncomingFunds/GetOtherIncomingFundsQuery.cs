using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.GetOtherIncomingFunds;

public sealed record GetOtherIncomingFundsQuery(Guid? GroupId = null) : IQuery<List<OtherIncomingFundResponse>>;

public sealed record OtherIncomingFundResponse(
    Guid Id,
    Guid MemberId,
    string MemberName,
    decimal Amount,
    DateTime PaidDate,
    string Remarks,
    bool IsVerified,
    DateTime? VerifiedAt,
    DateTime CreatedAt);
