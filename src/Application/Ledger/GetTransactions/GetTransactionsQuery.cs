using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Ledger;
using HamroSavings.SharedKernel;

namespace HamroSavings.Application.Ledger.GetTransactions;

public sealed record GetTransactionsQuery(
    Guid? GroupId = null,
    TransactionType? Type = null,
    LedgerAccount? Account = null,
    Guid? MemberId = null,
    DateTime? From = null,
    DateTime? To = null,
    /// <summary>"Debit" or "Credit", as the caller sees it: did the group's cash fall or rise.</summary>
    string? Side = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<TransactionResponse>>;

public sealed record TransactionResponse(
    Guid Id,
    DateTime OccurredAt,
    TransactionType Type,
    string Description,
    LedgerAccount DebitAccount,
    LedgerAccount CreditAccount,
    /// <summary>
    /// The everyday reading, as on a bank statement: "Credit" when the group's cash went
    /// up, "Debit" when it went down. Derived from the accounts, so it cannot disagree
    /// with the underlying entry.
    /// </summary>
    string Side,
    decimal Amount,
    Guid? MemberId,
    string? MemberName,
    string SourceType,
    Guid SourceId);
