using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Savings;

public sealed record DepositVerifiedDomainEvent(Guid DepositId, Guid MemberId, Guid GroupId) : IDomainEvent;
