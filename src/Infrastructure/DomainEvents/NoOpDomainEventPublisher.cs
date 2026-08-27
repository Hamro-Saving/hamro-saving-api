using HamroSavings.SharedKernel;

namespace HamroSavings.Infrastructure.DomainEvents;

/// <summary>
/// For the design-time factory, which builds a context only so <c>dotnet ef</c> can read the
/// model — it never saves, and has no host to run handlers in.
/// </summary>
internal sealed class NoOpDomainEventPublisher : IDomainEventPublisher
{
    public void Publish(IReadOnlyCollection<IDomainEvent> domainEvents) { }
}
