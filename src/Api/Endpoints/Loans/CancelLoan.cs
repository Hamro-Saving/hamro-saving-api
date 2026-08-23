using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.CancelLoan;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class CancelLoan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("loans/{id:guid}/cancel", async (
            Guid id,
            ICommandHandler<CancelLoanCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CancelLoanCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Cancel a loan before its disbursement starts (Admin/SuperAdmin only)");
    }
}
