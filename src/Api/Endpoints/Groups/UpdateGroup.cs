using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Groups.Update;

namespace HamroSavings.Api.Endpoints.Groups;

public sealed class UpdateGroup : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("groups/{id:guid}", async (
            Guid id,
            UpdateGroupRequest request,
            ICommandHandler<UpdateGroupCommand> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateGroupCommand(
                id,
                request.Name,
                request.Description,
                request.MemberInterestRate,
                request.NonMemberInterestRate);

            var result = await handler.Handle(command, ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Groups")
        .RequireAuthorization()
        .WithSummary("Update group settings");
    }
}

public sealed record UpdateGroupRequest(
    string Name,
    string? Description,
    decimal MemberInterestRate,
    decimal NonMemberInterestRate);
