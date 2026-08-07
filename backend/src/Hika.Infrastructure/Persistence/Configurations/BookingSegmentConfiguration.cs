using Hika.Domain.Bookings;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class BookingSegmentConfiguration : IEntityTypeConfiguration<BookingSegment>
{
    public void Configure(EntityTypeBuilder<BookingSegment> builder)
    {
        builder.ToTable("booking_segments");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => new { s.BookingId, s.TripSegmentId }).IsUnique();

        builder.HasOne<Booking>().WithMany().HasForeignKey(s => s.BookingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TripSegment>().WithMany().HasForeignKey(s => s.TripSegmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
