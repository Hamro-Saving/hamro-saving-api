using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.GetFinancialSummary;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class GetFinancialSummary : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("finance/summary", async (
            Guid? groupId,
            IQueryHandler<GetFinancialSummaryQuery, FinancialSummaryResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetFinancialSummaryQuery(groupId), ct);
            return result.Match(
                summary => Results.Ok(summary),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization()
        .WithSummary("Get overall financial summary");
    }
}
