using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.GetLoanSummary;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class GetLoanSummary : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("loans/summary", async (
            Guid? groupId,
            IQueryHandler<GetLoanSummaryQuery, LoanSummaryResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetLoanSummaryQuery(groupId), ct);
            return result.Match(
                summary => Results.Ok(summary),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Get loan summary");
    }
}
