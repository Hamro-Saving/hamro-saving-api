using HamroSavings.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;

namespace HamroSavings.Infrastructure.DomainEvents;

/// <summary>
/// Each event gets a fresh DI scope — the request that raised it is gone, and its DbContext
/// with it. A handler that throws is logged and the rest still run: the transaction has
/// already committed, so there is nothing to fail, and one bad address must not cost the others.
/// </summary>
internal sealed class DomainEventProcessor(
    DomainEventQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<DomainEventProcessor> logger)
    : BackgroundService
{
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo Handle)> Dispatch = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var domainEvent in queue.Reader.ReadAllAsync(stoppingToken))
            {
                await HandleAsync(domainEvent, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var (handlerType, handle) = Dispatch.GetOrAdd(domainEvent.GetType(), static eventType =>
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            return (handlerType, handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle))!);
        });

        using var scope = scopeFactory.CreateScope();

        foreach (var handler in scope.ServiceProvider.GetServices(handlerType))
        {
            if (handler is null) continue;

            try
            {
                await (Task)handle.Invoke(handler, [domainEvent, cancellationToken])!;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Domain event handler {Handler} failed for {DomainEvent}",
                    handler.GetType().Name,
                    domainEvent.GetType().Name);
            }
        }
    }
}
