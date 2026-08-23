using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.GetPayments;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class GetLoanPayments : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("loans/{id:guid}/payments", async (
            Guid id,
            IQueryHandler<GetLoanPaymentsQuery, List<LoanPaymentResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetLoanPaymentsQuery(id), ct);
            return result.Match(
                payments => Results.Ok(payments),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Get a loan's payment history with the interest each payment settled");
    }
}
