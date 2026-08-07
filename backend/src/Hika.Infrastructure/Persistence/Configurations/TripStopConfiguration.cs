using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class TripStopConfiguration : IEntityTypeConfiguration<TripStop>
{
    public void Configure(EntityTypeBuilder<TripStop> builder)
    {
        builder.ToTable("trip_stops");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.RawName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Province).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => new { s.TripId, s.Sequence }).IsUnique();
        builder.HasIndex(s => s.LocationId);

        builder.HasOne<Location>().WithMany().HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.SetNull);
    }
}
