using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.Update;
using HamroSavings.Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace HamroSavings.Api.Endpoints.Members;

internal sealed class UpdateMember : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("members/{id:guid}", async (
            Guid id,
            [FromBody] UpdateMemberRequest request,
            ICommandHandler<UpdateMemberCommand> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateMemberCommand(id, request.FirstName, request.LastName, request.Email, request.Role);
            var result = await handler.Handle(command, ct);
            return result.Match(() => Results.NoContent(), CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags("Members");
    }
}

internal sealed record UpdateMemberRequest(string FirstName, string LastName, string Email, UserRole Role);
