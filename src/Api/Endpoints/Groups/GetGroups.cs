using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Groups.Get;

namespace HamroSavings.Api.Endpoints.Groups;

public sealed class GetGroups : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("groups", async (
            IQueryHandler<GetGroupsQuery, List<GroupResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGroupsQuery(), ct);
            return result.Match(
                groups => Results.Ok(groups),
                error => CustomResults.Problem(error));
        })
        .WithTags("Groups")
        .RequireAuthorization()
        .WithSummary("Get all groups");
    }
}
