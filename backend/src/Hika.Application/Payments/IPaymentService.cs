using Hika.Application.Payments.Dtos;
using Hika.Domain.Common;

namespace Hika.Application.Payments;

public interface IPaymentService
{
    /// <summary>Creates and charges a Payment for a just-accepted booking. Adds the entity to
    /// the current unit of work without saving — the caller (BookingService.AcceptAsync) saves
    /// once, atomically, alongside the booking's own status change.</summary>
    Task CapturePaymentAsync(Guid bookingId, Money fare, CancellationToken cancellationToken);

    Task<PaymentResponse> GetForBookingAsync(Guid callerId, Guid bookingId, CancellationToken cancellationToken);

    Task<PaymentResponse> RefundAsync(Guid driverUserId, Guid bookingId, string reason, CancellationToken cancellationToken);

    /// <summary>Admin-initiated refund — no driver-ownership check, see docs/api-design.md's
    /// Admin financial-oversight section.</summary>
    Task<PaymentResponse> AdminRefundAsync(Guid bookingId, string reason, CancellationToken cancellationToken);
}
