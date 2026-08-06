using Hika.Domain.Common;

namespace Hika.Application.Common.Events;

/// <summary>
/// Lets a module react to another module's domain event (e.g. Notifications reacting to
/// Bookings' BookingConfirmed) without taking a direct project/namespace dependency on it.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
