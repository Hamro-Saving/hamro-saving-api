using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.GetExpenses;

namespace HamroSavings.Api.Endpoints.Finance;

public sealed class GetExpenses : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("expenses", async (
            Guid? groupId,
            string? category,
            DateTime? fromDate,
            DateTime? toDate,
            IQueryHandler<GetExpensesQuery, List<ExpenseResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetExpensesQuery(groupId, category, fromDate, toDate), ct);
            return result.Match(
                expenses => Results.Ok(expenses),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization()
        .WithSummary("Get expenses");
    }
}
