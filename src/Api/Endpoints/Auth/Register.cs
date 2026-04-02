using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Auth.Register;
using HamroSavings.Domain.Users;

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
                request.LastName,
                request.Role,
                request.GroupId);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/members/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Auth")
        .RequireAuthorization()
        .WithSummary("Register a new user (Admin/SuperAdmin only)");
    }
}

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid? GroupId);
