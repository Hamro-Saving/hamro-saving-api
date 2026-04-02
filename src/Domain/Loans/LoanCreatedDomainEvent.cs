using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

public sealed record LoanCreatedDomainEvent(Guid LoanId, Guid BorrowerId, Guid GroupId) : IDomainEvent;
