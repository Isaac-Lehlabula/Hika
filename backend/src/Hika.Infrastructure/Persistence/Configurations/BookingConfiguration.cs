using Hika.Domain.Bookings;
using Hika.Domain.Trips;
using Hika.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.CancellationReason).HasMaxLength(500);

        builder.ComplexProperty(b => b.TotalPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_price_amount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("total_price_currency").HasConversion<string>().HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(b => b.TripId);
        builder.HasIndex(b => new { b.PassengerUserId, b.Status });
        // Supports AdminBookingService's status-filtered, RequestedAtUtc-sorted list — the
        // (PassengerUserId, Status) index above doesn't help a Status-only filter (Status isn't
        // the leftmost column), same reasoning as Trip's (Status, DepartureAtUtc) index.
        builder.HasIndex(b => new { b.Status, b.RequestedAtUtc });

        builder.HasOne<Trip>().WithMany().HasForeignKey(b => b.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserProfile>().WithMany().HasForeignKey(b => b.PassengerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TripStop>().WithMany().HasForeignKey(b => b.BoardingStopId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TripStop>().WithMany().HasForeignKey(b => b.AlightingStopId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Passengers).WithOne().HasForeignKey(p => p.BookingId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(b => b.Passengers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
