using Hika.Domain.Common;

namespace Hika.Domain.Bookings;

/// <summary>
/// One person travelling on a Booking — for the driver's manifest/safety purposes (who is
/// actually in the car), not a billing unit (SeatsRequested is). The first (and, for MVP,
/// only) row is always the account holder who made the booking.
/// </summary>
public sealed class BookingPassenger : Entity
{
    public Guid BookingId { get; private set; }

    public string FullName { get; private set; }

    public string? PhoneNumber { get; private set; }

    public bool IsAccountHolder { get; private set; }

    private BookingPassenger()
    {
        FullName = string.Empty;
    }

    internal BookingPassenger(Guid bookingId, string fullName, string? phoneNumber, bool isAccountHolder)
    {
        BookingId = bookingId;
        FullName = fullName;
        PhoneNumber = phoneNumber;
        IsAccountHolder = isAccountHolder;
    }
}
