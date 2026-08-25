using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Ledger;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Ledger.GetTransactions;

internal sealed class GetTransactionsQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetTransactionsQuery, PagedResult<TransactionResponse>>
{
    public async Task<Result<PagedResult<TransactionResponse>>> Handle(GetTransactionsQuery query, CancellationToken cancellationToken = default)
    {
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<PagedResult<TransactionResponse>>(groupResult.Error);
        var groupId = groupResult.Value;

        var entries = dbContext.LedgerEntries.AsQueryable();

        if (groupId.HasValue)
            entries = entries.Where(e => e.GroupId == groupId.Value);

        if (query.Type.HasValue)
            entries = entries.Where(e => e.Type == query.Type.Value);

        // Either side of the entry counts as touching the account.
        if (query.Account.HasValue)
            entries = entries.Where(e => e.DebitAccount == query.Account.Value || e.CreditAccount == query.Account.Value);

        if (query.MemberId.HasValue)
            entries = entries.Where(e => e.MemberId == query.MemberId.Value);

        // Side is derived from which account cash sits on, so it filters on the same rule
        // the response reports — and must be applied here, not after paging.
        if (query.Side == "Credit")
            entries = entries.Where(e => e.DebitAccount == LedgerAccount.Cash);
        else if (query.Side == "Debit")
            entries = entries.Where(e => e.CreditAccount == LedgerAccount.Cash);

        if (query.From.HasValue)
            entries = entries.Where(e => e.OccurredAt >= query.From.Value);

        if (query.To.HasValue)
            entries = entries.Where(e => e.OccurredAt <= query.To.Value);

        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        // Counted before paging, so the caller knows the size of the whole result.
        var total = await entries.CountAsync(cancellationToken);

        // Asking past the end returns the last page rather than nothing, which is what a
        // caller wants when a filter has just shrunk the result under them.
        var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var page = Math.Clamp(query.Page, 1, lastPage);

        var rows = await entries
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.CreatedAt)
            // Id breaks remaining ties, so a row cannot shift between pages.
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new TransactionResponse(
                e.Id,
                e.OccurredAt,
                e.Type,
                e.Description,
                e.DebitAccount,
                e.CreditAccount,
                e.DebitAccount == LedgerAccount.Cash ? "Credit" : "Debit",
                e.Amount,
                e.MemberId,
                dbContext.Members
                    .Where(m => m.Id == e.MemberId)
                    .Select(m => m.LastName == null ? m.FirstName : m.FirstName + " " + m.LastName)
                    .FirstOrDefault(),
                e.SourceType,
                e.SourceId))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<TransactionResponse>(rows, page, pageSize, total));
    }
}
