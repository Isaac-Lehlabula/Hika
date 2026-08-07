using Hika.Application.Common.Persistence;
using Hika.Domain.Common;
using Hika.Domain.Drivers;
using Hika.Domain.Trips;
using Hika.Domain.TrustSafety;
using Hika.Domain.Users;
using Hika.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Hika.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Every Domain entity uses a client-generated Guid key (Guid.CreateVersion7(), see
        // Entity) — the database never generates one. This matters beyond documentation:
        // without it, EF Core's Added-vs-Unchanged heuristic for entities discovered via a
        // collection navigation (e.g. Vehicle.Photos — added by mutating the collection, not
        // via an explicit db.Set.Add() call) assumes a non-default key means "already exists
        // in the database" and issues an UPDATE for what is actually a brand new row.
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
