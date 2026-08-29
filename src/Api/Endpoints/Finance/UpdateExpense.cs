using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.DeleteExpense;
using HamroSavings.Application.Finance.UpdateExpense;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class UpdateExpense : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("expenses/{id:guid}", async (
            Guid id,
            UpdateExpenseRequest request,
            ICommandHandler<UpdateExpenseCommand> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateExpenseCommand(
                id, request.Amount, request.Category, request.Description, request.ExpenseDate);

            var result = await handler.Handle(command, ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Correct an unverified expense (group admin only)");
    }
}

public sealed class DeleteExpense : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("expenses/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteExpenseCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteExpenseCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization(Policies.GroupAdmin)
        .WithSummary("Delete an unverified expense (group admin only)");
    }
}

public sealed record UpdateExpenseRequest(
    decimal Amount,
    string Category,
    string Description,
    DateTime ExpenseDate);
