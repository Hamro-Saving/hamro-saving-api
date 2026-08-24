using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Savings.VerifyDeposit;

namespace HamroSavings.Api.Endpoints.Savings;

public sealed class VerifyDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("deposits/{id:guid}/verify", async (
            Guid id,
            ICommandHandler<VerifyDepositCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyDepositCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Savings")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Verify a deposit (group admin only)");
    }
}
