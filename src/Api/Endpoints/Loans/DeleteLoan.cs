using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.DeleteLoan;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class DeleteLoan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("loans/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteLoanCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteLoanCommand(id), ct);
            return result.Match(() => Results.NoContent(), CustomResults.Problem);
        })
        .WithTags("Loans")
        .RequireAuthorization(Policies.GroupMember)
        .WithSummary("Delete a cancelled loan (the borrower, or a group admin)");
    }
}
