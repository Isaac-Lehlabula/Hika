using Hika.Application.Bookings.Dtos;
using Hika.Application.Chat;
using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.Notifications;
using Hika.Application.Payments;
using Hika.Application.Trips;
using Hika.Domain.Bookings;
using Hika.Domain.Notifications;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Bookings;

public sealed class BookingService(
    IAppDbContext db, IPaymentService paymentService, INotificationDispatcher notificationDispatcher, IChatService chatService)
    : IBookingService
{
    public async Task<BookingResponse> RequestAsync(Guid passengerUserId, CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var trip = await db.Trips.Include(t => t.Stops).FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        if (trip.Status != TripStatus.Scheduled)
        {
            throw new AppValidationException("tripId", "This trip is no longer accepting bookings.");
        }

        if (trip.DriverProfileId == passengerUserId)
        {
            throw new AppValidationException("tripId", "You can't book your own trip.");
        }

        // One-directional Block rows still block booking both ways — see Block.cs.
        var isBlocked = await db.Blocks.AnyAsync(
            b => (b.BlockerUserId == passengerUserId && b.BlockedUserId == trip.DriverProfileId)
                || (b.BlockerUserId == trip.DriverProfileId && b.BlockedUserId == passengerUserId),
            cancellationToken);
        if (isBlocked)
        {
            throw new ForbiddenException("You can't book this trip.");
        }

        var boardingStop = trip.Stops.FirstOrDefault(s => s.Sequence == request.BoardingStopSequence)
            ?? throw new AppValidationException("boardingStopSequence", "That boarding stop doesn't exist on this trip.");
        var alightingStop = trip.Stops.FirstOrDefault(s => s.Sequence == request.AlightingStopSequence)
            ?? throw new AppValidationException("alightingStopSequence", "That alighting stop doesn't exist on this trip.");

        var passengerProfile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == passengerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user has no profile.");

        var totalPrice = trip.PricePerSeat * request.SeatsRequested;

        await using var transaction = await db.BeginTransactionAsync(cancellationToken);

        // Postgres advisory lock scoped to this trip — serializes every concurrent booking
        // attempt on it so no request can act on a stale SeatsAvailable read. See
        // docs/domain-model.md §4.3 for why this is trip-scoped rather than per-segment
        // row locking (a fixed, trivially-correct critical section vs. needing a consistent
        // lock-acquisition order across variable, overlapping stop ranges).
        await db.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [trip.Id.ToString()], cancellationToken);

        var sequenceByStopId = trip.Stops.ToDictionary(s => s.Id, s => s.Sequence);
        var allSegments = await db.TripSegments.Where(s => s.TripId == trip.Id).ToListAsync(cancellationToken);
        var segmentsInRange = allSegments
            .Where(s => sequenceByStopId[s.FromStopId] >= request.BoardingStopSequence && sequenceByStopId[s.ToStopId] <= request.AlightingStopSequence)
            .ToList();

        if (segmentsInRange.Count == 0 || segmentsInRange.Any(s => s.SeatsAvailable < request.SeatsRequested))
        {
            throw new ConflictException("Not enough seats available for the requested range.");
        }

        foreach (var segment in segmentsInRange)
        {
            segment.Reserve(request.SeatsRequested);
        }

        var booking = Booking.Request(
            trip.Id,
            passengerUserId,
            boardingStop.Id,
            alightingStop.Id,
            request.SeatsRequested,
            totalPrice,
            $"{passengerProfile.FirstName} {passengerProfile.LastName}",
            passengerProfile.PhoneNumber);

        db.Bookings.Add(booking);
        db.BookingSegments.AddRange(segmentsInRange.Select(s => new BookingSegment(booking.Id, s.Id)));

        await notificationDispatcher.DispatchAsync(
            trip.DriverProfileId,
            NotificationType.BookingRequested,
            $"{passengerProfile.FirstName} {passengerProfile.LastName} requested {request.SeatsRequested} seat(s) on your trip.",
            booking.Id,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await BuildResponseAsync(booking.Id, cancellationToken)
            ?? throw new InvalidOperationException("Booking was created but could not be re-read.");
    }

    public async Task<BookingResponse> GetAsync(Guid callerId, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        if (booking.PassengerUserId != callerId)
        {
            var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken);
            if (trip is null || trip.DriverProfileId != callerId)
            {
                throw new NotFoundException(nameof(Booking), bookingId);
            }
        }

        return await BuildResponseAsync(bookingId, cancellationToken) ?? throw new NotFoundException(nameof(Booking), bookingId);
    }

    public async Task<IReadOnlyList<BookingResponse>> GetMyBookingsAsync(Guid passengerUserId, CancellationToken cancellationToken)
    {
        var bookings = await db.Bookings
            .Where(b => b.PassengerUserId == passengerUserId)
            .ToListAsync(cancellationToken);

        return await BuildResponsesAsync(bookings, cancellationToken);
    }

    public async Task<IReadOnlyList<BookingResponse>> GetTripRequestsAsync(Guid driverUserId, Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken)
            ?? throw new NotFoundException(nameof(Trip), tripId);

        if (trip.DriverProfileId != driverUserId)
        {
            throw new NotFoundException(nameof(Trip), tripId);
        }

        var bookings = await db.Bookings.Where(b => b.TripId == tripId).ToListAsync(cancellationToken);

        return await BuildResponsesAsync(bookings, cancellationToken);
    }

    public async Task<BookingResponse> AcceptAsync(Guid driverUserId, Guid bookingId, CancellationToken cancellationToken)
    {
        var (booking, _) = await LoadOwnedByDriverAsync(driverUserId, bookingId, cancellationToken);

        booking.Accept();
        await chatService.OpenForBookingAsync(booking.Id, cancellationToken);

        await notificationDispatcher.DispatchAsync(
            booking.PassengerUserId,
            NotificationType.BookingAccepted,
            "Your booking request was accepted! Complete payment to confirm your seat.",
            booking.Id,
            cancellationToken);

        // Added to the same unit of work as the Accept status change. If the gateway settles
        // synchronously (Mock), the booking is confirmed and everything saves together below,
        // same as before AwaitingPayment/Ozow existed. If it doesn't (Ozow), the booking stays
        // AwaitingPayment and this saves just the Accept + a Pending Payment — the passenger
        // completes payment externally and OzowWebhooksController resolves it later via
        // ResolvePaymentOutcomeAsync.
        var paymentOutcome = await paymentService.InitiatePaymentAsync(booking.Id, booking.TotalPrice, cancellationToken);

        if (paymentOutcome.IsPending)
        {
            await db.SaveChangesAsync(cancellationToken);
            return await BuildResponseAsync(booking.Id, cancellationToken)
                ?? throw new InvalidOperationException("Booking disappeared mid-update.");
        }

        return await ResolvePaymentOutcomeAsync(booking.Id, paymentOutcome.Succeeded, providerReference: null, cancellationToken);
    }

    public async Task<BookingResponse> ResolvePaymentOutcomeAsync(
        Guid bookingId, bool succeeded, string? providerReference, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        if (booking.Status != BookingStatus.AwaitingPayment)
        {
            return await BuildResponseAsync(booking.Id, cancellationToken)
                ?? throw new InvalidOperationException("Booking disappeared mid-update.");
        }

        // A no-op when this booking's Payment was already resolved inline by
        // InitiatePaymentAsync (the Mock/synchronous path calling this straight from
        // AcceptAsync) — PaymentService.ResolvePaymentAsync guards on Payment.Status itself
        // already being non-Pending. For the Ozow/webhook path this is the actual resolution.
        await paymentService.ResolvePaymentAsync(bookingId, succeeded, providerReference, cancellationToken);

        if (succeeded)
        {
            booking.ConfirmPayment();
            await notificationDispatcher.DispatchAsync(
                booking.PassengerUserId, NotificationType.PaymentSucceeded, "Your payment was processed successfully.", booking.Id, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            booking.FailPayment();
            await chatService.CloseForBookingAsync(booking.Id, cancellationToken);
            await notificationDispatcher.DispatchAsync(
                booking.PassengerUserId,
                NotificationType.BookingDeclined,
                "Your payment could not be completed, so this booking was declined.",
                booking.Id,
                cancellationToken);
            await ReleaseSeatsAsync(booking.TripId, booking.Id, booking.SeatsRequested, cancellationToken);
        }

        return await BuildResponseAsync(booking.Id, cancellationToken)
            ?? throw new InvalidOperationException("Booking disappeared mid-update.");
    }

    public async Task<BookingResponse> DeclineAsync(Guid driverUserId, Guid bookingId, CancellationToken cancellationToken)
    {
        var (booking, trip) = await LoadOwnedByDriverAsync(driverUserId, bookingId, cancellationToken);

        booking.Decline();
        await notificationDispatcher.DispatchAsync(
            booking.PassengerUserId, NotificationType.BookingDeclined, "Your booking request was declined.", booking.Id, cancellationToken);
        await ReleaseSeatsAsync(trip.Id, booking.Id, booking.SeatsRequested, cancellationToken);

        return await BuildResponseAsync(booking.Id, cancellationToken)
            ?? throw new InvalidOperationException("Booking disappeared mid-update.");
    }

    public async Task<BookingResponse> CancelAsync(Guid passengerUserId, Guid bookingId, string? reason, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        if (booking.PassengerUserId != passengerUserId)
        {
            throw new NotFoundException(nameof(Booking), bookingId);
        }

        booking.Cancel(reason);
        await chatService.CloseForBookingAsync(booking.Id, cancellationToken);
        await ReleaseSeatsAsync(booking.TripId, booking.Id, booking.SeatsRequested, cancellationToken);

        return await BuildResponseAsync(booking.Id, cancellationToken)
            ?? throw new InvalidOperationException("Booking disappeared mid-update.");
    }

    public async Task<BookingResponse> CompleteAsync(Guid driverUserId, Guid bookingId, CancellationToken cancellationToken)
    {
        var (booking, trip) = await LoadOwnedByDriverAsync(driverUserId, bookingId, cancellationToken);

        if (trip.DepartureAtUtc > DateTimeOffset.UtcNow)
        {
            throw new AppValidationException("bookingId", "Can't complete a booking before the trip has departed.");
        }

        booking.Complete();
        await chatService.CloseForBookingAsync(booking.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await BuildResponseAsync(booking.Id, cancellationToken)
            ?? throw new InvalidOperationException("Booking disappeared mid-update.");
    }

    private async Task<(Booking Booking, Trip Trip)> LoadOwnedByDriverAsync(Guid driverUserId, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken)
            ?? throw new InvalidOperationException("Booking references a trip that no longer exists.");

        if (trip.DriverProfileId != driverUserId)
        {
            throw new NotFoundException(nameof(Booking), bookingId);
        }

        return (booking, trip);
    }

    /// <summary>Re-acquires the same trip-scoped advisory lock used by RequestAsync before
    /// incrementing SeatsAvailable back — see docs/domain-model.md §4.3's "on decline/cancellation"
    /// note. The caller has already applied the booking's own status transition (which validates
    /// it's legal) before calling this, so both land in the same SaveChanges/commit.</summary>
    private async Task ReleaseSeatsAsync(Guid tripId, Guid bookingId, int seatsRequested, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        await db.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [tripId.ToString()], cancellationToken);

        var segmentIds = await db.BookingSegments
            .Where(s => s.BookingId == bookingId)
            .Select(s => s.TripSegmentId)
            .ToListAsync(cancellationToken);
        var segments = await db.TripSegments.Where(s => segmentIds.Contains(s.Id)).ToListAsync(cancellationToken);

        foreach (var segment in segments)
        {
            segment.Release(seatsRequested);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<BookingResponse?> BuildResponseAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        var responses = await BuildResponsesAsync([booking], cancellationToken);
        return responses.FirstOrDefault();
    }

    private async Task<IReadOnlyList<BookingResponse>> BuildResponsesAsync(IReadOnlyList<Booking> bookings, CancellationToken cancellationToken)
    {
        if (bookings.Count == 0)
        {
            return [];
        }

        var tripIds = bookings.Select(b => b.TripId).Distinct().ToList();
        var trips = await db.Trips.Include(t => t.Stops).Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);

        var driverIds = trips.Values.Select(t => t.DriverProfileId).Distinct().ToList();
        var driverSummaries = await TripDisplayHelpers.BuildDriverSummariesAsync(db, driverIds, cancellationToken);

        var vehicleIds = trips.Values.Select(t => t.VehicleId).Distinct().ToList();
        var vehicles = await db.Vehicles.Include(v => v.Photos).Where(v => vehicleIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, cancellationToken);

        var locations = await TripDisplayHelpers.LoadLocationsAsync(db, trips.Values.SelectMany(t => t.Stops), cancellationToken);

        var passengerIds = bookings.Select(b => b.PassengerUserId).Distinct().ToList();
        var passengerProfiles = await db.UserProfiles.Where(p => passengerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        var responses = new List<BookingResponse>();
        foreach (var booking in bookings.OrderByDescending(b => b.RequestedAtUtc))
        {
            if (!trips.TryGetValue(booking.TripId, out var trip)
                || !driverSummaries.TryGetValue(trip.DriverProfileId, out var driverSummary)
                || !vehicles.TryGetValue(trip.VehicleId, out var vehicle)
                || !passengerProfiles.TryGetValue(booking.PassengerUserId, out var passengerProfile))
            {
                continue;
            }

            var boardingStop = trip.Stops.First(s => s.Id == booking.BoardingStopId);
            var alightingStop = trip.Stops.First(s => s.Id == booking.AlightingStopId);

            responses.Add(new BookingResponse
            {
                Id = booking.Id,
                Status = booking.Status.ToString(),
                Trip = new BookingTripSummary
                {
                    TripId = trip.Id,
                    DepartureAtUtc = trip.DepartureAtUtc,
                    OriginName = TripDisplayHelpers.ResolveStopName(trip.Stops[0], locations),
                    DestinationName = TripDisplayHelpers.ResolveStopName(trip.Stops[^1], locations),
                    Driver = driverSummary,
                    Vehicle = TripDisplayHelpers.ToVehicleSummary(vehicle),
                },
                BoardingStopSequence = boardingStop.Sequence,
                BoardingStopName = TripDisplayHelpers.ResolveStopName(boardingStop, locations),
                AlightingStopSequence = alightingStop.Sequence,
                AlightingStopName = TripDisplayHelpers.ResolveStopName(alightingStop, locations),
                SeatsRequested = booking.SeatsRequested,
                TotalPrice = booking.TotalPrice.Amount,
                Passenger = new BookingPassengerSummary
                {
                    UserId = passengerProfile.Id,
                    FirstName = passengerProfile.FirstName,
                    LastName = passengerProfile.LastName,
                    PhotoUrl = passengerProfile.PhotoUrl,
                    PhoneNumber = passengerProfile.PhoneNumber,
                },
                RequestedAtUtc = booking.RequestedAtUtc,
                RespondedAtUtc = booking.RespondedAtUtc,
                CancelledAtUtc = booking.CancelledAtUtc,
                CancellationReason = booking.CancellationReason,
            });
        }

        return responses;
    }
}
