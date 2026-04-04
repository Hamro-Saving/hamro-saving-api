using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.VerifyPayment;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class VerifyLoanPayment : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("loan-payments/{id:guid}/verify", async (
            Guid id,
            ICommandHandler<VerifyLoanPaymentCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyLoanPaymentCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Verify a loan payment (Admin/SuperAdmin only)");
    }
}

