using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.SetActive;

namespace HamroSavings.Api.Endpoints.Members;

public sealed class DeactivateMember : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("members/{id:guid}/deactivate", async (
            Guid id,
            ICommandHandler<SetMemberActiveCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new SetMemberActiveCommand(id, IsActive: false), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Members")
        .RequireAuthorization()
        .WithSummary("Take a member out of the group, keeping their record (group admin or SuperAdmin)");
    }
}

public sealed class ActivateMember : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("members/{id:guid}/activate", async (
            Guid id,
            ICommandHandler<SetMemberActiveCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new SetMemberActiveCommand(id, IsActive: true), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Members")
        .RequireAuthorization()
        .WithSummary("Put a member back into the group (group admin or SuperAdmin)");
    }
}
