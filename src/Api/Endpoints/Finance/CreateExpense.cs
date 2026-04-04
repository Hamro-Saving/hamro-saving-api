using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.CreateExpense;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class CreateExpense : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("expenses", async (
            CreateExpenseRequest request,
            ICommandHandler<CreateExpenseCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new CreateExpenseCommand(
                request.GroupId,
                request.Amount,
                request.Category,
                request.Description,
                request.ExpenseDate);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/expenses/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization()
        .WithSummary("Record an expense");
    }
}

public sealed record CreateExpenseRequest(
    Guid GroupId,
    decimal Amount,
    string Category,
    string Description,
    DateTime ExpenseDate);
