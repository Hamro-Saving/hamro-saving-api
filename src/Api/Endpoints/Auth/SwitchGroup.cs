using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Auth.SwitchGroup;

namespace HamroSavings.Api.Endpoints.Auth;

public sealed class SwitchGroup : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/switch-group", async (
            SwitchGroupRequest request,
            ICommandHandler<SwitchGroupCommand, string> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new SwitchGroupCommand(request.GroupId), ct);
            return result.Match(
                token => Results.Ok(new { Token = token }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Auth")
        .RequireAuthorization()
        .WithSummary("Re-issue the caller's token with a different group active");
    }
}

public sealed record SwitchGroupRequest(Guid GroupId);
