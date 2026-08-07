using Hika.Domain.RideAlerts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class RideAlertConfiguration : IEntityTypeConfiguration<RideAlert>
{
    public void Configure(EntityTypeBuilder<RideAlert> builder)
    {
        builder.ToTable("ride_alerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.OriginRawText).HasMaxLength(100).IsRequired();
        builder.Property(a => a.DestinationRawText).HasMaxLength(100).IsRequired();

        builder.HasIndex(a => a.Status);
    }
}
