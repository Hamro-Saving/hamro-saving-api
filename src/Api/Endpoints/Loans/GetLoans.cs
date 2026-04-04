using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.GetLoans;
using HamroSavings.Domain.Loans;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class GetLoans : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("loans", async (
            Guid? groupId,
            Guid? borrowerId,
            LoanStatus? status,
            IQueryHandler<GetLoansQuery, List<LoanResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetLoansQuery(groupId, borrowerId, status), ct);
            return result.Match(
                loans => Results.Ok(loans),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Get loans with optional filters");
    }
}
