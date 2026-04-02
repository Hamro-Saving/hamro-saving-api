using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.NonMembers.Update;
using HamroSavings.Application.NonMembers.Delete;
using Microsoft.AspNetCore.Mvc;

namespace HamroSavings.Api.Endpoints.NonMembers;

internal sealed class UpdateNonMember : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("non-members/{id:guid}", async (
            Guid id,
            [FromBody] UpdateNonMemberRequest request,
            ICommandHandler<UpdateNonMemberCommand> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateNonMemberCommand(id, request.FullName, request.Email, request.Phone, request.Address);
            var result = await handler.Handle(command, ct);
            return result.Match(() => Results.NoContent(), CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags("NonMembers");
    }
}

internal sealed class DeleteNonMember : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("non-members/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteNonMemberCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteNonMemberCommand(id), ct);
            return result.Match(() => Results.NoContent(), CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags("NonMembers");
    }
}

internal sealed record UpdateNonMemberRequest(
    string FullName,
    string? Email,
    string? Phone,
    string? Address);
