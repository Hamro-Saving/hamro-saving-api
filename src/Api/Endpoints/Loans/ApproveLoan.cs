using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.ApproveLoan;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class ApproveLoan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("loans/{id:guid}/approve", async (
            Guid id,
            ICommandHandler<ApproveLoanCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new ApproveLoanCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Approve a loan (member vote or admin instant-approve)");
    }
}
