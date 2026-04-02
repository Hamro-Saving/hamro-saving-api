using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.CreateLoan;
using HamroSavings.Application.Loans.GetLoanById;
using HamroSavings.Application.Loans.GetLoanSummary;
using HamroSavings.Application.Loans.GetLoans;
using HamroSavings.Application.Loans.RecordPayment;
using HamroSavings.Application.Loans.VerifyPayment;
using HamroSavings.Domain.Loans;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class CreateLoan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("loans", async (
            CreateLoanRequest request,
            ICommandHandler<CreateLoanCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new CreateLoanCommand(
                request.BorrowerId,
                request.BorrowerType,
                request.GroupId,
                request.Amount,
                request.InterestRate,
                request.StartDate,
                request.DueDate,
                request.Notes);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/loans/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Create a new loan");
    }
}

public sealed class GetLoans : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("loans", async (
            Guid? groupId,
            Guid? borrowerId,
            LoanStatus? status,
            IQueryHandler<GetLoansQuery, List<LoanResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetLoansQuery(groupId, borrowerId, status), ct);
            return result.Match(
                loans => Results.Ok(loans),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Get loans with optional filters");
    }
}

public sealed class GetLoanById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("loans/{id:guid}", async (
            Guid id,
            IQueryHandler<GetLoanByIdQuery, LoanResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetLoanByIdQuery(id), ct);
            return result.Match(
                loan => Results.Ok(loan),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Get loan by ID");
    }
}

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

public sealed class VerifyLoanPayment : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("loan-payments/{id:guid}/verify", async (
            Guid id,
            VerifyPaymentRequest request,
            ICommandHandler<VerifyLoanPaymentCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new VerifyLoanPaymentCommand(id, request.GroupId), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Verify a loan payment (Admin/SuperAdmin only)");
    }
}

public sealed class GetLoanSummary : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("loans/summary", async (
            Guid? groupId,
            IQueryHandler<GetLoanSummaryQuery, LoanSummaryResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetLoanSummaryQuery(groupId), ct);
            return result.Match(
                summary => Results.Ok(summary),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization()
        .WithSummary("Get loan summary");
    }
}

public sealed record CreateLoanRequest(
    Guid BorrowerId,
    string BorrowerType,
    Guid GroupId,
    decimal Amount,
    decimal? InterestRate,
    DateTime StartDate,
    DateTime? DueDate,
    string? Notes);

public sealed record RecordPaymentRequest(
    Guid GroupId,
    decimal Amount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    DateTime PaidDate,
    LoanPaymentType PaymentType,
    string? Notes);

public sealed record VerifyPaymentRequest(Guid GroupId);
