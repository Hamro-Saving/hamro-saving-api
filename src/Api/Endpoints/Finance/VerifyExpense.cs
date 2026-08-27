using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.VerifyExpense;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class VerifyExpense : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("expenses/{id:guid}/verify", async (
            Guid id,
            ICommandHandler<VerifyExpenseCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyExpenseCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Verify an expense, posting it to the ledger (group admin only)");
    }
}
