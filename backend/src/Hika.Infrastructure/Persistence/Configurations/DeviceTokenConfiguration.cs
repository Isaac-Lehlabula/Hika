using Hika.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("device_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).HasMaxLength(4096).IsRequired();
        builder.Property(t => t.Platform).HasConversion<string>().HasMaxLength(10);

        // Unique so DeviceTokenService.RegisterAsync's lookup-then-reassign never has two rows
        // racing for the same physical device's token.
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.UserId);
    }
}
