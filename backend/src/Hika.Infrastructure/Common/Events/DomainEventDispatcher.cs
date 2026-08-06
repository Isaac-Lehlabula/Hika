using Hika.Application.Common.Events;
using Hika.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hika.Infrastructure.Common.Events;

/// <summary>
/// Resolves and invokes IDomainEventHandler&lt;TEvent&gt; instances from DI for each raised event.
/// Runs handlers synchronously, in-process, within the same request — sufficient at MVP scale;
/// swapping in an outbox/message bus later only touches this class.
/// </summary>
public sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                logger.LogDebug(
                    "Dispatching {DomainEvent} to {Handler}",
                    domainEvent.GetType().Name,
                    handler.GetType().Name);

                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
