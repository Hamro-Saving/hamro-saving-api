using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.CastVote;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class ApproveLoan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("loans/{id:guid}/approve", async (
            Guid id,
            ICommandHandler<CastLoanVoteCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CastLoanVoteCommand(id, IsApproved: true), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Approve a loan (member vote or admin instant-approve)");
    }
}
