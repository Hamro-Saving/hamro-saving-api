using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

/// <summary>
/// A loan has been put to the group and is waiting on a vote. Raised on a fresh request and
/// again on a revision, which clears the votes and so asks the group the question afresh.
/// </summary>
public sealed record LoanRequestedDomainEvent(Guid LoanId, Guid GroupId, Guid BorrowerId) : IDomainEvent;
