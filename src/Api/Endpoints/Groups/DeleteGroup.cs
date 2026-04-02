using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Groups.Delete;

namespace HamroSavings.Api.Endpoints.Groups;

public sealed class DeleteGroup : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("groups/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteGroupCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteGroupCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Groups")
        .RequireAuthorization()
        .WithSummary("Delete a group (SuperAdmin only, group must have no data)");
    }
}
