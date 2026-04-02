using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Groups.Get;
using HamroSavings.Application.Groups.GetById;

namespace HamroSavings.Api.Endpoints.Groups;

public sealed class GetGroupById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("groups/{id:guid}", async (
            Guid id,
            IQueryHandler<GetGroupByIdQuery, GroupResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGroupByIdQuery(id), ct);
            return result.Match(
                group => Results.Ok(group),
                error => CustomResults.Problem(error));
        })
        .WithTags("Groups")
        .RequireAuthorization()
        .WithSummary("Get group by ID");
    }
}
