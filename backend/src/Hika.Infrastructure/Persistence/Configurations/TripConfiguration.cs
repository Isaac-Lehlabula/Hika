using Hika.Domain.Drivers;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.LuggageAllowance).HasMaxLength(200);
        builder.Property(t => t.Notes).HasMaxLength(1000);

        // Money is a value object (readonly record struct), not an entity — ComplexProperty
        // (EF Core 8+) is the correct mapping for that, not OwnsOne (which targets entity
        // types with their own identity/tracking).
        builder.ComplexProperty(t => t.PricePerSeat, money =>
        {
            money.Property(m => m.Amount).HasColumnName("price_per_seat_amount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("price_per_seat_currency").HasConversion<string>().HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(t => new { t.Status, t.DepartureAtUtc });
        builder.HasIndex(t => t.DriverProfileId);

        builder.HasOne<DriverProfile>().WithMany().HasForeignKey(t => t.DriverProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(t => t.VehicleId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Stops).WithOne().HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Stops).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Segments).WithOne().HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Segments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
