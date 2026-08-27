namespace HamroSavings.SharedKernel;

/// <summary>
/// Fire-and-forget by contract: a notification that fails must never fail the transaction
/// that caused it.
/// </summary>
public interface IDomainEventPublisher
{
    void Publish(IReadOnlyCollection<IDomainEvent> domainEvents);
}
