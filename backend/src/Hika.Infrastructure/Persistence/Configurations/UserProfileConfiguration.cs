using Hika.Domain.Users;
using Hika.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PhotoUrl).HasMaxLength(2048);
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.AverageRating).HasColumnType("decimal(3,2)");

        builder.HasIndex(p => p.PhoneNumber)
            .IsUnique()
            .HasFilter("\"phone_number\" IS NOT NULL");

        // Shared-primary-key 1:1 with the Identity user. No CLR navigation on either side —
        // UserProfile only ever holds the Guid, per docs/architecture.md's module-boundary rule.
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
