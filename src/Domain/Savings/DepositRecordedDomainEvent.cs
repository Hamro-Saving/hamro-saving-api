using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Savings;

/// <summary>
/// A deposit has been entered but not yet verified. The group is not told about it — the
/// figure can still be corrected or deleted — but the admins who have to verify it are.
/// </summary>
public sealed record DepositRecordedDomainEvent(Guid DepositId, Guid MemberId, Guid GroupId) : IDomainEvent;
