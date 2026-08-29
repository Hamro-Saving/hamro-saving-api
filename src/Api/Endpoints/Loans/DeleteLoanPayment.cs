using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.DeletePayment;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class DeleteLoanPayment : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("loan-payments/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteLoanPaymentCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteLoanPaymentCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Delete an unverified loan payment (group admin only)");
    }
}
