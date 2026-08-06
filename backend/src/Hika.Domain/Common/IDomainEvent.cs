namespace Hika.Domain.Common;

/// <summary>
/// Marker for an event raised by an entity as a result of a state change, dispatched
/// after the change is persisted so other modules can react without a direct reference.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
