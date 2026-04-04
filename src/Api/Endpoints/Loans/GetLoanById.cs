using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.GetLoanById;
using HamroSavings.Application.Loans.GetLoans;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class GetLoanById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("loans/{id:guid}", async (
            Guid id,
            IQueryHandler<GetLoanByIdQuery, LoanResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetLoanByIdQuery(id), ct);
            return result.Match(
                loan => Results.Ok(loan),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Get loan by ID");
    }
}
