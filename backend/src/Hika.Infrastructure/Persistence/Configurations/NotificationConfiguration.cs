using Hika.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(10);
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(10);
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();

        builder.HasIndex(n => new { n.UserId, n.CreatedAtUtc });
    }
}
