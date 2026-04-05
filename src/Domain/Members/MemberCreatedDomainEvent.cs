using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Members;

public sealed record MemberCreatedDomainEvent(Guid MemberId) : IDomainEvent;
