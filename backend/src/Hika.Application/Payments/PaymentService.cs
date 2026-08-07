using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.Payments.Dtos;
using Hika.Application.Payments.Ports;
using Hika.Domain.Admin;
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

        var feeRate = await GetCurrentFeeRateAsync(cancellationToken);
        var payment = Payment.Charge(bookingId, fare, feeRate);
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

        return await RefundCoreAsync(bookingId, reason, cancellationToken);
    }

    /// <summary>Same refund flow as <see cref="RefundAsync"/> but without the driver-ownership
    /// check — for admin financial oversight (see docs/api-design.md's Admin section), which
    /// is authorized by the caller's Admin policy instead, not by who owns the trip.</summary>
    public Task<PaymentResponse> AdminRefundAsync(Guid bookingId, string reason, CancellationToken cancellationToken) =>
        RefundCoreAsync(bookingId, reason, cancellationToken);

    private async Task<PaymentResponse> RefundCoreAsync(Guid bookingId, string reason, CancellationToken cancellationToken)
    {
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

    /// <summary>Lazily seeds the singleton settings row at its default rate the first time
    /// it's needed — see PlatformFeeSettings.CreateDefault — so a fresh database doesn't need
    /// a separate seeding step before the very first booking can be accepted.</summary>
    private async Task<decimal> GetCurrentFeeRateAsync(CancellationToken cancellationToken)
    {
        var settings = await db.PlatformFeeSettings.FirstOrDefaultAsync(s => s.Id == PlatformFeeSettings.SingletonId, cancellationToken);
        if (settings is not null)
        {
            return settings.Rate;
        }

        settings = PlatformFeeSettings.CreateDefault();
        db.PlatformFeeSettings.Add(settings);
        return settings.Rate;
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
