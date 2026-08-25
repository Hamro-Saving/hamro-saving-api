using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Savings.DeleteDeposit;

namespace HamroSavings.Api.Endpoints.Savings;

public sealed class DeleteDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("deposits/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteDepositCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteDepositCommand(id), ct);
            return result.Match(() => Results.NoContent(), CustomResults.Problem);
        })
        .WithTags("Savings")
        .RequireAuthorization(Policies.GroupMember)
        .WithSummary("Delete an unverified deposit (the member it belongs to, or a group admin)");
    }
}
