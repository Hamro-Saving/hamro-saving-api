using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Finance;

/// <summary>Money has been placed with an institution and is waiting on an admin to check it.</summary>
public sealed record FixedDepositRecordedDomainEvent(Guid FixedDepositId, Guid GroupId) : IDomainEvent;

public sealed record FixedDepositVerifiedDomainEvent(Guid FixedDepositId, Guid GroupId) : IDomainEvent;

/// <summary>
/// The money has been recorded as coming back. Separate from the placement's own events: a
/// withdrawal is a second movement of money on the same record and is checked on its own.
/// </summary>
public sealed record FixedDepositWithdrawalRecordedDomainEvent(Guid FixedDepositId, Guid GroupId) : IDomainEvent;

public sealed record FixedDepositWithdrawalVerifiedDomainEvent(Guid FixedDepositId, Guid GroupId) : IDomainEvent;
