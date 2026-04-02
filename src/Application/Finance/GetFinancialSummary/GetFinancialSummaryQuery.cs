using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Finance.GetFinancialSummary;

public sealed record GetFinancialSummaryQuery(Guid? GroupId = null) : IQuery<FinancialSummaryResponse>;
