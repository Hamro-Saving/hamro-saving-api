using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.AssignAdmin;

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
        .WithSummary("Assign a member as group admin (SuperAdmin only, demotes existing admin)");
    }
}
