using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.Payments.Dtos;
using Hika.Application.Payments.Ports;
using Hika.Domain.Bookings;
using Hika.Domain.Common;
using Hika.Domain.Payments;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Payments;

public sealed class PaymentService(IAppDbContext db, IPaymentGateway paymentGateway) : IPaymentService
{
    public async Task CapturePaymentAsync(Guid bookingId, Money fare, CancellationToken cancellationToken)
    {
        var gatewayResult = await paymentGateway.InitiateChargeAsync(bookingId, fare, cancellationToken);

        var payment = Payment.Charge(bookingId, fare);
        if (gatewayResult.Succeeded)
        {
            payment.MarkSucceeded(gatewayResult.ProviderReference!);
        }
        else
        {
            payment.MarkFailed();
        }

        db.Payments.Add(payment);
    }

    public async Task<PaymentResponse> GetForBookingAsync(Guid callerId, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        await EnsureCallerCanViewAsync(callerId, booking, cancellationToken);

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken)
            ?? throw new NotFoundException("Payment for booking", bookingId);

        return ToResponse(payment);
    }

    public async Task<PaymentResponse> RefundAsync(Guid driverUserId, Guid bookingId, string reason, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken)
            ?? throw new InvalidOperationException("Booking references a trip that no longer exists.");

        if (trip.DriverProfileId != driverUserId)
        {
            throw new NotFoundException(nameof(Booking), bookingId);
        }

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken)
            ?? throw new NotFoundException("Payment for booking", bookingId);

        if (payment.Status != PaymentStatus.Succeeded)
        {
            throw new ConflictException($"Cannot refund a payment that is {payment.Status}.");
        }

        var gatewayResult = await paymentGateway.RefundAsync(payment.ProviderReference!, payment.Amount, cancellationToken);

        var refund = Refund.Request(payment.Id, payment.Amount, reason);
        if (gatewayResult.Succeeded)
        {
            refund.MarkSucceeded();
            payment.MarkRefunded();
        }
        else
        {
            refund.MarkFailed();
        }

        db.Refunds.Add(refund);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(payment);
    }

    private async Task EnsureCallerCanViewAsync(Guid callerId, Booking booking, CancellationToken cancellationToken)
    {
        if (booking.PassengerUserId == callerId)
        {
            return;
        }

        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken);
        if (trip is null || trip.DriverProfileId != callerId)
        {
            throw new NotFoundException(nameof(Booking), booking.Id);
        }
    }

    private static PaymentResponse ToResponse(Payment payment) => new()
    {
        Id = payment.Id,
        BookingId = payment.BookingId,
        Amount = payment.Amount.Amount,
        PlatformFee = payment.PlatformFee.Amount,
        DriverPayoutAmount = payment.DriverPayoutAmount.Amount,
        Provider = payment.Provider.ToString(),
        ProviderReference = payment.ProviderReference,
        Status = payment.Status.ToString(),
        CreatedAtUtc = payment.CreatedAtUtc,
    };
}
