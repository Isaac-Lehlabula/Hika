using Hika.Application.Common.Persistence;
using Hika.Domain.Bookings;
using Hika.Domain.Common;
using Hika.Domain.Drivers;
using Hika.Domain.Trips;
using Hika.Domain.TrustSafety;
using Hika.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Hika.UnitTests.TestSupport;

/// <summary>
/// A minimal EF Core InMemory-backed IAppDbContext for Application-layer unit tests — keeps
/// AuthService/UserProfileService tests fast and free of Docker, while still exercising real
/// LINQ query behavior (unlike hand-mocking DbSet&lt;T&gt;, which doesn't support queries at all).
/// Integration tests separately exercise the real Postgres-backed AppDbContext.
/// </summary>
public sealed class InMemoryAppDbContext : DbContext, IAppDbContext
{
    /// <param name="databaseName">
    /// Defaults to a fresh unique database. Pass an explicit name shared across multiple
    /// InMemoryAppDbContext instances to simulate separate request-scoped DbContexts reading
    /// the same underlying data — e.g. testing a service method called twice "as if" by two
    /// separate HTTP requests, which is how it actually runs in production (a fresh DbContext
    /// per request) but not how a single reused instance behaves under the InMemory provider.
    /// </param>
    public InMemoryAppDbContext(string? databaseName = null)
        : base(new DbContextOptionsBuilder<InMemoryAppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<PhoneVerificationCode> PhoneVerificationCodes => Set<PhoneVerificationCode>();

    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Verification> Verifications => Set<Verification>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<TripStop> TripStops => Set<TripStop>();

    public DbSet<TripSegment> TripSegments => Set<TripSegment>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingPassenger> BookingPassengers => Set<BookingPassenger>();

    public DbSet<BookingSegment> BookingSegments => Set<BookingSegment>();

    // The InMemory provider doesn't support transactions or raw SQL — BookingService.RequestAsync,
    // which needs both for the advisory-locked reservation, is covered by the Postgres
    // integration tests instead. These exist only so InMemoryAppDbContext satisfies
    // IAppDbContext; the explicit exception is clearer than letting a relational-only
    // extension method fail for an unrelated reason.
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException("InMemoryAppDbContext doesn't support transactions.");

    public Task ExecuteSqlRawAsync(string sql, object[] parameters, CancellationToken cancellationToken) =>
        throw new NotSupportedException("InMemoryAppDbContext doesn't support raw SQL.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>().HasKey(p => p.Id);
        modelBuilder.Entity<RefreshToken>().HasKey(t => t.Id);
        modelBuilder.Entity<EmailVerificationToken>().HasKey(t => t.Id);
        modelBuilder.Entity<PasswordResetToken>().HasKey(t => t.Id);
        modelBuilder.Entity<PhoneVerificationCode>().HasKey(c => c.Id);

        modelBuilder.Entity<DriverProfile>().HasKey(p => p.Id);

        modelBuilder.Entity<Vehicle>(builder =>
        {
            builder.HasKey(v => v.Id);
            builder.HasMany(v => v.Photos).WithOne().HasForeignKey(p => p.VehicleId);
            builder.Navigation(v => v.Photos).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<VehiclePhoto>().HasKey(p => p.Id);

        modelBuilder.Entity<Verification>().HasKey(v => v.Id);

        modelBuilder.Entity<Location>().HasKey(l => l.Id);

        modelBuilder.Entity<Trip>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.ComplexProperty(t => t.PricePerSeat);
            builder.HasMany(t => t.Stops).WithOne().HasForeignKey(s => s.TripId);
            builder.Navigation(t => t.Stops).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(t => t.Segments).WithOne().HasForeignKey(s => s.TripId);
            builder.Navigation(t => t.Segments).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TripStop>().HasKey(s => s.Id);

        modelBuilder.Entity<TripSegment>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.ComplexProperty(s => s.PriceOverride);
        });

        modelBuilder.Entity<Booking>(builder =>
        {
            builder.HasKey(b => b.Id);
            builder.ComplexProperty(b => b.TotalPrice);
            builder.HasMany(b => b.Passengers).WithOne().HasForeignKey(p => p.BookingId);
            builder.Navigation(b => b.Passengers).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<BookingPassenger>().HasKey(p => p.Id);

        modelBuilder.Entity<BookingSegment>().HasKey(s => s.Id);

        // Same client-generated-key convention as production — see AppDbContext for why.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var idProperty = entityType.FindProperty(nameof(Entity.Id));
            if (idProperty is not null)
            {
                idProperty.ValueGenerated = ValueGenerated.Never;
            }
        }
    }
}
