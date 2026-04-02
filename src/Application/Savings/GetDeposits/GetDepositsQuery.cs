using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Savings;

namespace HamroSavings.Application.Savings.GetDeposits;

public sealed record GetDepositsQuery(
    Guid? GroupId = null,
    Guid? MemberId = null,
    int? Month = null,
    int? Year = null,
    bool? IsVerified = null) : IQuery<List<DepositResponse>>;
