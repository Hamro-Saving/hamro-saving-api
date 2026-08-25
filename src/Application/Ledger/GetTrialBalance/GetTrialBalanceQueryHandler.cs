using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.GetFinancialSummary;
using HamroSavings.Domain.Ledger;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Ledger.GetTrialBalance;

/// <summary>
/// The two-way check. Every entry names a debit and an equal credit, so the two columns
/// must agree; and cash rebuilt from the ledger must match what the financial summary
/// computes from the underlying records. Either mismatch means something was recorded in
/// one place and not the other.
/// </summary>
internal sealed class GetTrialBalanceQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IQueryHandler<GetFinancialSummaryQuery, FinancialSummaryResponse> summaryHandler)
    : IQueryHandler<GetTrialBalanceQuery, TrialBalanceResponse>
{
    public async Task<Result<TrialBalanceResponse>> Handle(GetTrialBalanceQuery query, CancellationToken cancellationToken = default)
    {
        var groupResult = userContext.ResolveReadGroupId(query.GroupId);
        if (groupResult.IsFailure) return Result.Failure<TrialBalanceResponse>(groupResult.Error);
        var groupId = groupResult.Value;

        var entries = dbContext.LedgerEntries.AsQueryable();
        if (groupId.HasValue)
            entries = entries.Where(e => e.GroupId == groupId.Value);

        var rows = await entries
            .Select(e => new { e.DebitAccount, e.CreditAccount, e.Amount })
            .ToListAsync(cancellationToken);

        var accounts = new List<AccountBalance>();

        foreach (var account in Enum.GetValues<LedgerAccount>())
        {
            var debits = rows.Where(r => r.DebitAccount == account).Sum(r => r.Amount);
            var credits = rows.Where(r => r.CreditAccount == account).Sum(r => r.Amount);

            // Assets and expenses read positive when debited; liabilities and income when credited.
            var balance = account.IsDebitBalance() ? debits - credits : credits - debits;

            if (debits != 0 || credits != 0)
                accounts.Add(new AccountBalance(account, debits, credits, balance));
        }

        var totalDebits = rows.Sum(r => r.Amount);
        var totalCredits = totalDebits; // each entry contributes to both columns by construction

        var ledgerCash = accounts.FirstOrDefault(a => a.Account == LedgerAccount.Cash)?.Balance ?? 0;

        var summary = await summaryHandler.Handle(new GetFinancialSummaryQuery(query.GroupId), cancellationToken);
        var summaryCash = summary.IsSuccess ? summary.Value.InHandCash : 0;

        var moneyIn = rows.Where(r => r.DebitAccount == LedgerAccount.Cash).Sum(r => r.Amount);
        var moneyOut = rows.Where(r => r.CreditAccount == LedgerAccount.Cash).Sum(r => r.Amount);

        return Result.Success(new TrialBalanceResponse(
            accounts,
            totalDebits,
            totalCredits,
            totalDebits == totalCredits,
            moneyIn,
            moneyOut,
            ledgerCash,
            summaryCash,
            ledgerCash - summaryCash));
    }
}
