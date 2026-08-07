using Hika.Domain.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Make).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Color).HasMaxLength(30).IsRequired();
        builder.Property(v => v.RegistrationNumber).HasMaxLength(20).IsRequired();

        builder.HasIndex(v => v.DriverProfileId);

        builder.HasOne<DriverProfile>()
            .WithMany()
            .HasForeignKey(v => v.DriverProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Photos)
            .WithOne()
            .HasForeignKey(p => p.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.Photos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
