using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.RecordPayment;
using HamroSavings.Domain.Loans;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class RecordLoanPayment : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("loans/{id:guid}/payments", async (
            Guid id,
            RecordPaymentRequest request,
            ICommandHandler<RecordLoanPaymentCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new RecordLoanPaymentCommand(
                id,
                request.GroupId,
                request.Amount,
                request.PrincipalAmount,
                request.InterestAmount,
                request.PaidDate,
                request.PaymentType,
                request.Notes);

            var result = await handler.Handle(command, ct);
            return result.Match(
                paymentId => Results.Created($"/api/v1/loans/{id}/payments/{paymentId}", new { Id = paymentId }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Record a loan payment");
    }
}

public sealed record RecordPaymentRequest(
    Guid GroupId,
    decimal Amount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    DateTime PaidDate,
    LoanPaymentType PaymentType,
    string? Notes);
