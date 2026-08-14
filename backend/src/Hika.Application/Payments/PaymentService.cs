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
    public async Task<PaymentInitiationOutcome> InitiatePaymentAsync(Guid bookingId, Money fare, CancellationToken cancellationToken)
    {
        var feeRate = await GetCurrentFeeRateAsync(cancellationToken);
        var payment = Payment.Charge(bookingId, fare, feeRate, paymentGateway.Provider);
        db.Payments.Add(payment);

        var gatewayResult = await paymentGateway.InitiatePaymentAsync(bookingId, fare, cancellationToken);

        if (gatewayResult.IsPending)
        {
            payment.SetRedirectUrl(gatewayResult.RedirectUrl!);
            return new PaymentInitiationOutcome { IsPending = true, RedirectUrl = gatewayResult.RedirectUrl };
        }

        if (gatewayResult.Succeeded)
        {
            payment.MarkSucceeded(gatewayResult.ProviderReference!);
        }
        else
        {
            payment.MarkFailed();
        }

        return new PaymentInitiationOutcome { IsPending = false, Succeeded = gatewayResult.Succeeded };
    }

    public async Task ResolvePaymentAsync(Guid bookingId, bool succeeded, string? providerReference, CancellationToken cancellationToken)
    {
        // Checks the change tracker before the database: when called inline from
        // BookingService.AcceptAsync (the Mock/synchronous path), the Payment InitiatePaymentAsync
        // just created is Added but not yet saved, so a DB query alone would never find it.
        // When called from OzowWebhooksController (a separate request/DbContext entirely), the
        // Payment was already saved by the time the webhook could possibly arrive, so the DB
        // fallback is what actually resolves it there.
        var payment = db.Payments.Local.FirstOrDefault(p => p.BookingId == bookingId)
            ?? await db.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken)
            ?? throw new NotFoundException("Payment for booking", bookingId);

        if (payment.Status != PaymentStatus.Pending)
        {
            // A retried/duplicate webhook delivery — Ozow resends a few times on error, and
            // this endpoint has no other way to tell "already processed" from "processing
            // again would double-apply a state change" without this check. Not an error.
            return;
        }

        if (succeeded)
        {
            payment.MarkSucceeded(providerReference ?? throw new AppValidationException("providerReference", "A successful payment must have a provider reference."));
        }
        else
        {
            payment.MarkFailed();
        }
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

    public async Task ResolveRefundAsync(Guid refundId, bool succeeded, CancellationToken cancellationToken)
    {
        var refund = await db.Refunds.FirstOrDefaultAsync(r => r.Id == refundId, cancellationToken)
            ?? throw new NotFoundException(nameof(Refund), refundId);

        if (refund.Status != RefundStatus.Pending)
        {
            return;
        }

        if (succeeded)
        {
            refund.MarkSucceeded();

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == refund.PaymentId, cancellationToken)
                ?? throw new InvalidOperationException("Refund references a payment that no longer exists.");
            payment.MarkRefunded();
        }
        else
        {
            refund.MarkFailed();
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<PaymentResponse> RefundCoreAsync(Guid bookingId, string reason, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken)
            ?? throw new NotFoundException("Payment for booking", bookingId);

        if (payment.Status != PaymentStatus.Succeeded)
        {
            throw new ConflictException($"Cannot refund a payment that is {payment.Status}.");
        }

        var refund = Refund.Request(payment.Id, payment.Amount, reason);
        db.Refunds.Add(refund);

        var gatewayResult = await paymentGateway.InitiateRefundAsync(refund.Id, payment.ProviderReference!, payment.Amount, cancellationToken);

        if (!gatewayResult.IsPending)
        {
            if (gatewayResult.Succeeded)
            {
                refund.MarkSucceeded();
                payment.MarkRefunded();
            }
            else
            {
                refund.MarkFailed();
            }
        }
        // else: stays Pending until OzowWebhooksController's refund-notify calls ResolveRefundAsync.

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
        RedirectUrl = payment.RedirectUrl,
        CreatedAtUtc = payment.CreatedAtUtc,
    };
}
