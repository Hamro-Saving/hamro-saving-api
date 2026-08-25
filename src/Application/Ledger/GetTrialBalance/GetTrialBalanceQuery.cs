using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Ledger;

namespace HamroSavings.Application.Ledger.GetTrialBalance;

public sealed record GetTrialBalanceQuery(Guid? GroupId = null) : IQuery<TrialBalanceResponse>;

public sealed record AccountBalance(
    LedgerAccount Account,
    decimal Debits,
    decimal Credits,
    /// <summary>Signed the way the account naturally runs, so it reads as a real figure.</summary>
    decimal Balance);

public sealed record TrialBalanceResponse(
    List<AccountBalance> Accounts,
    decimal TotalDebits,
    decimal TotalCredits,
    /// <summary>True when the books prove out. Anything else means an entry was lost.</summary>
    bool IsBalanced,
    /// <summary>Everything that put money into the group's cash.</summary>
    decimal MoneyIn,
    /// <summary>Everything that took money out of it.</summary>
    decimal MoneyOut,
    /// <summary>Cash as the ledger sees it.</summary>
    decimal LedgerCash,
    /// <summary>Cash as the existing summary computes it, for a second opinion.</summary>
    decimal SummaryCash,
    /// <summary>Any gap between the two. Zero is the healthy answer.</summary>
    decimal CashDifference);
