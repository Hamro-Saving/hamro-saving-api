using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.AssignAdmin;
using HamroSavings.Application.Members.RemoveAdmin;
using HamroSavings.Application.Members.ResendInvite;

namespace HamroSavings.Api.Endpoints.Members;

public sealed class AssignAdmin : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("members/{id:guid}/assign-admin", async (
            Guid id,
            ICommandHandler<AssignAdminCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new AssignAdminCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Members")
        .RequireAuthorization()
        .WithSummary("Assign a member as group admin (group admin or SuperAdmin)");
    }
}

public sealed class RemoveAdmin : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("members/{id:guid}/remove-admin", async (
            Guid id,
            ICommandHandler<RemoveAdminCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new RemoveAdminCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Members")
        .RequireAuthorization()
        .WithSummary("Demote a group admin back to member (group admin or SuperAdmin)");
    }
}

public sealed class ResendInvite : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("members/{id:guid}/resend-invite", async (
            Guid id,
            ICommandHandler<ResendInviteCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new ResendInviteCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Members")
        .RequireAuthorization()
        .WithSummary("Resend invite email to a member who hasn't activated their account (group admin or SuperAdmin)");
    }
}
