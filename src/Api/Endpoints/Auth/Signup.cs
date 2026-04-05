using HamroSavings.Api.Infrastructure;
using HamroSavings.Api.Extensions;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Auth.GetSignupInfo;
using HamroSavings.Application.Auth.Signup;

namespace HamroSavings.Api.Endpoints.Auth;

public sealed class SignupWithToken : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/signup", async (
            SignupRequest request,
            ICommandHandler<SignupWithTokenCommand, string> handler,
            CancellationToken ct) =>
        {
            var command = new SignupWithTokenCommand(request.Token, request.Password);
            var result = await handler.Handle(command, ct);
            return result.Match(
                token => Results.Ok(new { Token = token }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Auth")
        .WithSummary("Complete member signup using an invite token")
        .AllowAnonymous();
    }
}

public sealed class GetSignupInfo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("auth/signup-info", async (
            Guid token,
            IQueryHandler<GetSignupInfoQuery, SignupInfoResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetSignupInfoQuery(token), ct);
            return result.Match(
                info => Results.Ok(info),
                error => CustomResults.Problem(error));
        })
        .WithTags("Auth")
        .WithSummary("Get member info for a signup token (public)")
        .AllowAnonymous();
    }
}

public sealed record SignupRequest(Guid Token, string Password);
