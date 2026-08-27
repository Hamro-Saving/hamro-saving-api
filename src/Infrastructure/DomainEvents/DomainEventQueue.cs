using HamroSavings.SharedKernel;
using System.Threading.Channels;

namespace HamroSavings.Infrastructure.DomainEvents;

/// <summary>
/// Non-blocking, so a request never waits on an email server.
///
/// In-memory only: an event still queued when the process stops is lost. Fine for
/// notifications, wrong for anything that must not be missed — a durable outbox is the
/// upgrade path if that day comes.
/// </summary>
internal sealed class DomainEventQueue : IDomainEventPublisher
{
    private readonly Channel<IDomainEvent> _channel = Channel.CreateUnbounded<IDomainEvent>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<IDomainEvent> Reader => _channel.Reader;

    public void Publish(IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            _channel.Writer.TryWrite(domainEvent);
        }
    }
}
