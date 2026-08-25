using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Domain.Ledger;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Ledger;

internal static class CashPosition
{
    /// <summary>
    /// What the group is actually holding, rebuilt from the ledger: everything debited to
    /// Cash less everything credited out of it. Taken from the books rather than recomputed
    /// from the underlying records, so a spending limit and the finance page can never
    /// disagree about how much there is.
    /// </summary>
    public static async Task<CashInHand> InHandAsync(
        IApplicationDbContext dbContext,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var entries = dbContext.LedgerEntries
            .Where(e => e.GroupId == groupId
                     && (e.DebitAccount == LedgerAccount.Cash || e.CreditAccount == LedgerAccount.Cash));

        var inflow = await entries
            .Where(e => e.DebitAccount == LedgerAccount.Cash)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;

        var outflow = await entries
            .Where(e => e.CreditAccount == LedgerAccount.Cash)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;

        return new CashInHand(inflow - outflow);
    }
}
