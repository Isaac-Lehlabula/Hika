using Hika.Domain.Drivers;
using Hika.Domain.Trips;
using Hika.Domain.TrustSafety;
using Hika.Domain.Users;
using Microsoft.EntityFrameworkCore;

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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
