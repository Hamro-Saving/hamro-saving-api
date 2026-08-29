using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.UpdatePayment;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class UpdateLoanPayment : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("loan-payments/{id:guid}", async (
            Guid id,
            UpdateLoanPaymentRequest request,
            ICommandHandler<UpdateLoanPaymentCommand> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateLoanPaymentCommand(
                id,
                request.PrincipalAmount,
                request.InterestAmount,
                request.PaidDate,
                request.Notes);

            var result = await handler.Handle(command, ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Correct an unverified loan payment (group admin only)");
    }
}

public sealed record UpdateLoanPaymentRequest(
    decimal PrincipalAmount,
    decimal InterestAmount,
    DateTime PaidDate,
    string? Notes);
