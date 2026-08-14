using Hika.Application.Payments.Dtos;
using Hika.Domain.Common;

namespace Hika.Application.Payments;

public interface IPaymentService
{
    /// <summary>Creates a Pending Payment for a just-accepted booking and asks the active
    /// gateway to start it. Adds the Payment to the current unit of work without saving — the
    /// caller (BookingService.AcceptAsync) saves once, atomically, alongside the booking's own
    /// status change. The returned outcome tells the caller whether the gateway already
    /// resolved it (see ResolvePaymentAsync, called inline for that case) or the caller needs
    /// to surface a RedirectUrl and wait for a webhook.</summary>
    Task<PaymentInitiationOutcome> InitiatePaymentAsync(Guid bookingId, Money fare, CancellationToken cancellationToken);

    /// <summary>Applies a payment outcome — from the synchronous path right after
    /// InitiatePaymentAsync (Mock) or from OzowWebhooksController later (Ozow). Only updates the
    /// Payment row; the caller (BookingService) is responsible for the corresponding Booking
    /// state transition, since that's a booking concern, not a payment one.</summary>
    Task ResolvePaymentAsync(Guid bookingId, bool succeeded, string? providerReference, CancellationToken cancellationToken);

    Task<PaymentResponse> GetForBookingAsync(Guid callerId, Guid bookingId, CancellationToken cancellationToken);

    Task<PaymentResponse> RefundAsync(Guid driverUserId, Guid bookingId, string reason, CancellationToken cancellationToken);

    /// <summary>Admin-initiated refund — no driver-ownership check, see docs/api-design.md's
    /// Admin financial-oversight section.</summary>
    Task<PaymentResponse> AdminRefundAsync(Guid bookingId, string reason, CancellationToken cancellationToken);

    /// <summary>Applies a refund outcome arriving asynchronously from OzowWebhooksController's
    /// refund-notify endpoint. A no-op for refunds that already settled synchronously (Mock),
    /// which never leave RefundAsync/AdminRefundAsync with a Pending refund to resolve later.</summary>
    Task ResolveRefundAsync(Guid refundId, bool succeeded, CancellationToken cancellationToken);
}
