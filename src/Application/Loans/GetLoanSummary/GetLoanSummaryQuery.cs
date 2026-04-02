using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.GetLoanSummary;

public sealed record GetLoanSummaryQuery(Guid? GroupId = null) : IQuery<LoanSummaryResponse>;
