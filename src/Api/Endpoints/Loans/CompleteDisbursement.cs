using HamroSavings.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.CompleteDisbursement;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class CompleteDisbursement : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("loans/{id:guid}/complete-disbursement", async (
            Guid id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DisburseRequest? request,
            ICommandHandler<CompleteDisbursementCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CompleteDisbursementCommand(id, request?.DisbursedOn, request?.DisbursedAmount), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Mark a loan's disbursement complete, activating it (group admin only)");
    }
}

/// <param name="DisbursedOn">The day the money reached the borrower. Omit for today.</param>
/// <param name="DisbursedAmount">What was handed over. Omit for the full approved amount.</param>
public sealed record DisburseRequest(DateOnly? DisbursedOn, decimal? DisbursedAmount);
