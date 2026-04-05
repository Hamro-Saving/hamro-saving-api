using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Auth.Register;

namespace HamroSavings.Api.Endpoints.Auth;

public sealed class Register : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/register", async (
            RegisterRequest request,
            ICommandHandler<RegisterCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new RegisterCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/users/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Auth")
        .RequireAuthorization("SuperAdmin")
        .WithSummary("Register a new SuperAdmin user (SuperAdmin only)");
    }
}

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName);
