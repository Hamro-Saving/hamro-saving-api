using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Groups.Create;

namespace HamroSavings.Api.Endpoints.Groups;

public sealed class CreateGroup : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("groups", async (
            CreateGroupRequest request,
            ICommandHandler<CreateGroupCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new CreateGroupCommand(
                request.Name,
                request.Code,
                request.Description,
                request.MemberInterestRate,
                request.NonMemberInterestRate);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/groups/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Groups")
        .RequireAuthorization()
        .WithSummary("Create a new group (SuperAdmin only)");
    }
}

public sealed record CreateGroupRequest(
    string Name,
    string Code,
    string? Description,
    decimal MemberInterestRate = 10m,
    decimal NonMemberInterestRate = 18m);
