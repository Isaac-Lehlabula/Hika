using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Province).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(l => l.Longitude).HasColumnType("decimal(9,6)");

        builder.HasIndex(l => l.Name);
    }
}
