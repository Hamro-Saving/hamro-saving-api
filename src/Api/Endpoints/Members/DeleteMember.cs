using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.Delete;

namespace HamroSavings.Api.Endpoints.Members;

internal sealed class DeleteMember : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("members/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteMemberCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteMemberCommand(id), ct);
            return result.Match(() => Results.NoContent(), CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags("Members");
    }
}
