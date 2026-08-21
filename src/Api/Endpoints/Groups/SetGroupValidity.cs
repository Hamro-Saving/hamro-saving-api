using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Groups.SetValidity;

namespace HamroSavings.Api.Endpoints.Groups;

public sealed class SetGroupValidity : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("groups/{id:guid}/validity", async (
            Guid id,
            SetGroupValidityRequest request,
            ICommandHandler<SetGroupValidityCommand> handler,
            CancellationToken ct) =>
        {
            var command = new SetGroupValidityCommand(id, request.IsActive, request.ValidFrom, request.ValidTo);
            var result = await handler.Handle(command, ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Groups")
        .RequireAuthorization()
        .WithSummary("Set group active status and validity period (SuperAdmin only)");
    }
}

public sealed record SetGroupValidityRequest(
    bool IsActive,
    DateTime? ValidFrom,
    DateTime? ValidTo);
