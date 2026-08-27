using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.VerifyFixedDeposit;
using HamroSavings.Application.Finance.VerifyFixedDepositWithdrawal;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class VerifyFixedDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("fixed-deposits/{id:guid}/verify", async (
            Guid id,
            ICommandHandler<VerifyFixedDepositCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyFixedDepositCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Verify a fixed deposit placement, posting it to the ledger (group admin only)");

        // The withdrawal is a second movement of money on the same record and is verified
        // separately, so it gets its own endpoint rather than a flag on the one above.
        app.MapPut("fixed-deposits/{id:guid}/verify-withdrawal", async (
            Guid id,
            ICommandHandler<VerifyFixedDepositWithdrawalCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyFixedDepositWithdrawalCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Verify a fixed deposit withdrawal and its interest (group admin only)");
    }
}
