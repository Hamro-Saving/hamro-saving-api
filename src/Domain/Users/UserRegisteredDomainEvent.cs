using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Users;

public sealed record UserRegisteredDomainEvent(Guid UserId) : IDomainEvent;
