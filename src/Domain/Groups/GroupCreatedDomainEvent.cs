using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Groups;

public sealed record GroupCreatedDomainEvent(Guid GroupId) : IDomainEvent;
