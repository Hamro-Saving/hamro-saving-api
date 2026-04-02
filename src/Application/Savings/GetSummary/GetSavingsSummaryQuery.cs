using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Savings.GetSummary;

public sealed record GetSavingsSummaryQuery(Guid? GroupId = null) : IQuery<SavingsSummaryResponse>;
