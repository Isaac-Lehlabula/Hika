using Hika.Domain.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class VehiclePhotoConfiguration : IEntityTypeConfiguration<VehiclePhoto>
{
    public void Configure(EntityTypeBuilder<VehiclePhoto> builder)
    {
        builder.ToTable("vehicle_photos");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Url).HasMaxLength(2048).IsRequired();

        builder.HasIndex(p => p.VehicleId);
    }
}
