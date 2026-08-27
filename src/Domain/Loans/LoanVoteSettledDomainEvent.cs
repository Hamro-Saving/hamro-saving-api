using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

/// <summary>The vote reached its threshold and the loan is now approved or declined.</summary>
public sealed record LoanVoteSettledDomainEvent(Guid LoanId, Guid GroupId, Guid BorrowerId, bool IsApproved) : IDomainEvent;
