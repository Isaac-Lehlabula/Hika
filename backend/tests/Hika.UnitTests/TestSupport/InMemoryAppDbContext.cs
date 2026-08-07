using Hika.Application.Common.Persistence;
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
    public InMemoryAppDbContext()
        : base(new DbContextOptionsBuilder<InMemoryAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<PhoneVerificationCode> PhoneVerificationCodes => Set<PhoneVerificationCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>().HasKey(p => p.Id);
        modelBuilder.Entity<RefreshToken>().HasKey(t => t.Id);
        modelBuilder.Entity<EmailVerificationToken>().HasKey(t => t.Id);
        modelBuilder.Entity<PasswordResetToken>().HasKey(t => t.Id);
        modelBuilder.Entity<PhoneVerificationCode>().HasKey(c => c.Id);
    }
}
