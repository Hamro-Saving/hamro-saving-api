using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.CastVote;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class DeclineLoan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("loans/{id:guid}/decline", async (
            Guid id,
            ICommandHandler<CastLoanVoteCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CastLoanVoteCommand(id, IsApproved: false), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization(Policies.GroupMember)
        .WithSummary("Decline a loan (member vote or admin instant-decline)");
    }
}
