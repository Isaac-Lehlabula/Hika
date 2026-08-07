using Hika.Domain.Bookings;
using Hika.Domain.Drivers;
using Hika.Domain.Payments;
using Hika.Domain.Reviews;
using Hika.Domain.Trips;
using Hika.Domain.TrustSafety;
using Hika.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Hika.Application.Common.Persistence;

/// <summary>
/// The seam between Application and the real EF Core DbContext (Infrastructure). Exposes
/// typed DbSets so application services can write normal EF LINQ queries (Include, Where,
/// projections) without Application depending on the concrete AppDbContext or on Identity —
/// this is the "repository, but not one-per-entity" compromise described in
/// docs/architecture.md §6: DbSet already is a repository, this just relocates the seam.
/// </summary>
public interface IAppDbContext
{
    DbSet<UserProfile> UserProfiles { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }

    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    DbSet<PhoneVerificationCode> PhoneVerificationCodes { get; }

    DbSet<DriverProfile> DriverProfiles { get; }

    DbSet<Vehicle> Vehicles { get; }

    DbSet<Verification> Verifications { get; }

    DbSet<Location> Locations { get; }

    DbSet<Trip> Trips { get; }

    DbSet<TripStop> TripStops { get; }

    DbSet<TripSegment> TripSegments { get; }

    DbSet<Booking> Bookings { get; }

    DbSet<BookingPassenger> BookingPassengers { get; }

    DbSet<BookingSegment> BookingSegments { get; }

    DbSet<Payment> Payments { get; }

    DbSet<Refund> Refunds { get; }

    DbSet<Review> Reviews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The concurrency-safe seat reservation (see docs/domain-model.md §4.3) needs an explicit
    /// transaction to hold a Postgres advisory lock (via <see cref="ExecuteSqlRawAsync"/>)
    /// across the read-check-decrement sequence. These two members are the seam's one
    /// deliberate crack from "DbSet already is a repository" — raw SQL and manual transactions
    /// aren't expressible as a DbSet, but both are still generic EF Core concepts, not
    /// Npgsql-specific ones, so this doesn't leak the concrete provider into Application.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    Task ExecuteSqlRawAsync(string sql, object[] parameters, CancellationToken cancellationToken);
}
