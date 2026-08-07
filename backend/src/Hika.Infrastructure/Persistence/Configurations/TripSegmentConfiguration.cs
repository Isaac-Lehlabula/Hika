using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class TripSegmentConfiguration : IEntityTypeConfiguration<TripSegment>
{
    public void Configure(EntityTypeBuilder<TripSegment> builder)
    {
        builder.ToTable("trip_segments");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.SeatsAvailable).IsRequired();

        builder.ComplexProperty(s => s.PriceOverride, money =>
        {
            money.Property(m => m.Amount).HasColumnName("price_override_amount").HasColumnType("decimal(18,2)");
            money.Property(m => m.Currency).HasColumnName("price_override_currency").HasConversion<string>().HasMaxLength(3);
        });

        builder.HasIndex(s => new { s.TripId, s.FromStopId, s.ToStopId }).IsUnique();

        builder.HasOne<TripStop>().WithMany().HasForeignKey(s => s.FromStopId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TripStop>().WithMany().HasForeignKey(s => s.ToStopId).OnDelete(DeleteBehavior.Restrict);
    }
}
