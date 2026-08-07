using Hika.Domain.Drivers;
using Hika.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class DriverProfileConfiguration : IEntityTypeConfiguration<DriverProfile>
{
    public void Configure(EntityTypeBuilder<DriverProfile> builder)
    {
        builder.ToTable("driver_profiles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.LicenseNumber).HasMaxLength(30).IsRequired();

        // Shared-primary-key 1:1 with the Identity user, same pattern as UserProfile.
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<DriverProfile>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
