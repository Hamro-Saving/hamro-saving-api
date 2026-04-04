using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.CreateLoan;

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

public sealed record CreateLoanRequest(
    Guid BorrowerId,
    string BorrowerType,
    Guid GroupId,
    decimal Amount,
    decimal? InterestRate,
    DateTime StartDate,
    DateTime? DueDate,
    string? Notes);
