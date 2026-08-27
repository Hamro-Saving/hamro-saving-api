using HamroSavings.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.ForceDisburseLoan;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class ForceDisburseLoan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("loans/{id:guid}/force-disburse", async (
            Guid id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DisburseRequest? request,
            ICommandHandler<ForceDisburseLoanCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new ForceDisburseLoanCommand(id, request?.DisbursedOn, request?.DisbursedAmount), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Disburse a loan the members never voted on (group admin only)");
    }
}
