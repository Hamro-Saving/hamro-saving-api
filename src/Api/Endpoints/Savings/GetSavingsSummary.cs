using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Savings.GetSummary;

namespace HamroSavings.Api.Endpoints.Savings;

public sealed class GetSavingsSummary : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("deposits/summary", async (
            Guid? groupId,
            IQueryHandler<GetSavingsSummaryQuery, SavingsSummaryResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetSavingsSummaryQuery(groupId), ct);
            return result.Match(
                summary => Results.Ok(summary),
                error => CustomResults.Problem(error));
        })
        .WithTags("Savings")
        .RequireAuthorization()
        .WithSummary("Get savings summary");
    }
}
