using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Auth.Login;

namespace HamroSavings.Api.Endpoints.Auth;

public sealed class Login : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/login", async (
            LoginRequest request,
            ICommandHandler<LoginCommand, string> handler,
            CancellationToken ct) =>
        {
            var command = new LoginCommand(request.Email, request.Password);
            var result = await handler.Handle(command, ct);
            return result.Match(
                token => Results.Ok(new { Token = token }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Auth")
        .AllowAnonymous()
        .WithSummary("Login and get a JWT token");
    }
}

public sealed record LoginRequest(string Email, string Password);
