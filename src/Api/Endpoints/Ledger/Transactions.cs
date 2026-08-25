using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Ledger.GetTransactions;
using HamroSavings.Application.Ledger.GetTrialBalance;
using HamroSavings.Domain.Ledger;
using HamroSavings.SharedKernel;

namespace HamroSavings.Api.Endpoints.Ledger;

public sealed class GetTransactions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("transactions", async (
            Guid? groupId,
            string? type,
            string? account,
            Guid? memberId,
            DateTime? from,
            DateTime? to,
            string? side,
            int? page,
            int? pageSize,
            IQueryHandler<GetTransactionsQuery, PagedResult<TransactionResponse>> handler,
            CancellationToken ct) =>
        {
            var query = new GetTransactionsQuery(
                groupId,
                type is null ? null : Enum.Parse<TransactionType>(type),
                account is null ? null : Enum.Parse<LedgerAccount>(account),
                memberId,
                from,
                to,
                side,
                page ?? 1,
                pageSize ?? 20);

            var result = await handler.Handle(query, ct);
            return result.Match(
                rows => Results.Ok(rows),
                error => CustomResults.Problem(error));
        })
        .WithTags("Transactions")
        .RequireAuthorization(Policies.GroupRead)
        .WithSummary("The group's ledger: every debit and credit, newest first");
    }
}

public sealed class GetTrialBalance : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("transactions/trial-balance", async (
            Guid? groupId,
            IQueryHandler<GetTrialBalanceQuery, TrialBalanceResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetTrialBalanceQuery(groupId), ct);
            return result.Match(
                balance => Results.Ok(balance),
                error => CustomResults.Problem(error));
        })
        .WithTags("Transactions")
        .RequireAuthorization(Policies.GroupRead)
        .WithSummary("Account balances, with debits and credits proved against each other");
    }
}
