using Hika.Domain.Common;

namespace Hika.Domain.Bookings;

/// <summary>Join row: exactly which TripSegment(s) a Booking consumes — see
/// docs/domain-model.md §4.2's worked example. Created by BookingService alongside the
/// Booking itself, inside the same advisory-locked transaction that reserves the seats.</summary>
public sealed class BookingSegment : Entity
{
    public Guid BookingId { get; private set; }

    public Guid TripSegmentId { get; private set; }

    private BookingSegment()
    {
    }

    public BookingSegment(Guid bookingId, Guid tripSegmentId)
    {
        BookingId = bookingId;
        TripSegmentId = tripSegmentId;
    }
}
