using Hika.Application.Common.Persistence;
using Hika.Domain.Drivers;
using Hika.Domain.TrustSafety;
using Hika.Domain.Users;
using Microsoft.EntityFrameworkCore;

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
    }
}
