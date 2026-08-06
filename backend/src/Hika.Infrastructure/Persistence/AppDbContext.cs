using Microsoft.EntityFrameworkCore;

namespace Hika.Infrastructure.Persistence;

/// <summary>
/// Phase 1: plumbing only (no entities yet) — proves the connection to Postgres and backs
/// the readiness health check. Becomes an IdentityDbContext with real DbSets from Phase 2 onward.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
