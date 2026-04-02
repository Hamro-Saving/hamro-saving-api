using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.GetFixedDeposits;

public sealed record GetFixedDepositsQuery(Guid? GroupId = null) : IQuery<List<FixedDepositResponse>>;
