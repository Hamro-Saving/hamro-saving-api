using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.UpdateLoan;

namespace HamroSavings.Api.Endpoints.Loans;

public sealed class UpdateLoan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("loans/{id:guid}", async (
            Guid id,
            UpdateLoanRequest request,
            ICommandHandler<UpdateLoanCommand> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateLoanCommand(id, request.Amount, request.InterestRate, request.StartDate, request.DueDate, request.Notes);
            var result = await handler.Handle(command, ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Loans")
        .RequireAuthorization(Policies.GroupMember)
        .WithSummary("Update an unapproved loan (borrower or Admin/SuperAdmin)");
    }
}

public sealed record UpdateLoanRequest(decimal Amount, decimal? InterestRate, DateTime StartDate, DateTime? DueDate, string? Notes);
