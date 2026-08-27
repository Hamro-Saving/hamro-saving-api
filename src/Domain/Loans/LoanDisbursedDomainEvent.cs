using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

/// <summary>The money has left the group and is with the borrower.</summary>
public sealed record LoanDisbursedDomainEvent(Guid LoanId, Guid GroupId, Guid BorrowerId) : IDomainEvent;
