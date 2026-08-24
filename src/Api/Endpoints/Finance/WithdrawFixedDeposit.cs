using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.WithdrawFixedDeposit;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class WithdrawFixedDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("fixed-deposits/{id:guid}/withdraw", async (
            Guid id,
            WithdrawFixedDepositRequest request,
            ICommandHandler<WithdrawFixedDepositCommand> handler,
            CancellationToken ct) =>
        {
            var command = new WithdrawFixedDepositCommand(id, request.InterestEarned, request.WithdrawnAt);

            var result = await handler.Handle(command, ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Withdraw a fixed deposit, recording the interest actually returned (group admin only)");
    }
}

public sealed record WithdrawFixedDepositRequest(
    decimal InterestEarned,
    DateTime WithdrawnAt);
