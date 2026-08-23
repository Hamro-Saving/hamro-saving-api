using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.CompleteDisbursement;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class CompleteDisbursement : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("loans/{id:guid}/complete-disbursement", async (
            Guid id,
            ICommandHandler<CompleteDisbursementCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CompleteDisbursementCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Mark a loan's disbursement complete, activating it (Admin/SuperAdmin only)");
    }
}
