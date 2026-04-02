using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.CreateExpense;
using HamroSavings.Application.Finance.CreateFixedDeposit;
using HamroSavings.Application.Finance.GetExpenses;
using HamroSavings.Application.Finance.GetFinancialSummary;
using HamroSavings.Application.Finance.GetFixedDeposits;

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

public sealed class CreateFixedDeposit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("fixed-deposits", async (
            CreateFixedDepositRequest request,
            ICommandHandler<CreateFixedDepositCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new CreateFixedDepositCommand(
                request.GroupId,
                request.InstitutionName,
                request.Amount,
                request.InterestRate,
                request.StartDate,
                request.MaturityDate,
                request.Notes);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/fixed-deposits/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization()
        .WithSummary("Create a fixed deposit record");
    }
}

public sealed class GetFixedDeposits : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("fixed-deposits", async (
            Guid? groupId,
            IQueryHandler<GetFixedDepositsQuery, List<FixedDepositResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetFixedDepositsQuery(groupId), ct);
            return result.Match(
                fds => Results.Ok(fds),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization()
        .WithSummary("Get fixed deposits");
    }
}

public sealed class GetFinancialSummary : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("finance/summary", async (
            Guid? groupId,
            IQueryHandler<GetFinancialSummaryQuery, FinancialSummaryResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetFinancialSummaryQuery(groupId), ct);
            return result.Match(
                summary => Results.Ok(summary),
                error => CustomResults.Problem(error));
        })
        .WithTags("Finance")
        .RequireAuthorization()
        .WithSummary("Get overall financial summary");
    }
}

public sealed record CreateExpenseRequest(
    Guid GroupId,
    decimal Amount,
    string Category,
    string Description,
    DateTime ExpenseDate);

public sealed record CreateFixedDepositRequest(
    Guid GroupId,
    string InstitutionName,
    decimal Amount,
    decimal InterestRate,
    DateTime StartDate,
    DateTime MaturityDate,
    string? Notes);
