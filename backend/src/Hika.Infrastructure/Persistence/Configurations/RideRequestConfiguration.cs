using Hika.Domain.RideRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class RideRequestConfiguration : IEntityTypeConfiguration<RideRequest>
{
    public void Configure(EntityTypeBuilder<RideRequest> builder)
    {
        builder.ToTable("ride_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.OriginRawText).HasMaxLength(100).IsRequired();
        builder.Property(r => r.DestinationRawText).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ProposedPricePerSeat).HasColumnType("decimal(18,2)");

        // Covers GetOpenRequestsAsync's Where(Status, TravelDate).OrderBy(TravelDate).
        builder.HasIndex(r => new { r.Status, r.TravelDate });
        builder.HasIndex(r => r.RiderUserId);
    }
}
