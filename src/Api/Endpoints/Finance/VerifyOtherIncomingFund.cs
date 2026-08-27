using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.VerifyOtherIncomingFund;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class VerifyOtherIncomingFund : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("other-incoming-funds/{id:guid}/verify", async (
            Guid id,
            ICommandHandler<VerifyOtherIncomingFundCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyOtherIncomingFundCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Verify an incoming funds record, posting it to the ledger (group admin only)");
    }
}
