using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Finance;

public sealed record OtherIncomingFundRecordedDomainEvent(Guid RecordId, Guid GroupId, Guid MemberId) : IDomainEvent;

public sealed record OtherIncomingFundVerifiedDomainEvent(Guid RecordId, Guid GroupId, Guid MemberId) : IDomainEvent;
