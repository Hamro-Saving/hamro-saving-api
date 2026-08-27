using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Finance;

/// <summary>An expense has been entered and is waiting on an admin to check it.</summary>
public sealed record ExpenseRecordedDomainEvent(Guid ExpenseId, Guid GroupId) : IDomainEvent;

/// <summary>An expense has been checked and is on the group's books.</summary>
public sealed record ExpenseVerifiedDomainEvent(Guid ExpenseId, Guid GroupId) : IDomainEvent;
