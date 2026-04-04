using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.VerifyLoan;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class VerifyLoan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("loans/{id:guid}/verify", async (
            Guid id,
            ICommandHandler<VerifyLoanCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyLoanCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Verify an approved loan (Admin/SuperAdmin only)");
    }
}
