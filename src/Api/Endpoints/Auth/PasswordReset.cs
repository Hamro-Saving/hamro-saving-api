using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Auth.ForgotPassword;
using HamroSavings.Application.Auth.ResetPassword;

namespace HamroSavings.Api.Endpoints.Auth;

public sealed class ForgotPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/forgot-password", async (
            ForgotPasswordRequest request,
            ICommandHandler<ForgotPasswordCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new ForgotPasswordCommand(request.Email), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Auth")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.AccountRecovery)
        .WithSummary("Email a password reset link, if the address has an active account")
        .WithDescription(
            "Answers the same way whether or not the address is known, so that it cannot be "
            + "used to find out who has an account. Only a malformed address is refused.");
    }
}

public sealed class ResetPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/reset-password", async (
            ResetPasswordRequest request,
            ICommandHandler<ResetPasswordCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new ResetPasswordCommand(request.Token, request.Password), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Auth")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.AccountRecovery)
        .WithSummary("Set a new password using a reset token");
    }
}

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(Guid Token, string Password);
