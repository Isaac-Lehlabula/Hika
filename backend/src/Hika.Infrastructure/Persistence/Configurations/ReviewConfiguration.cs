using Hika.Domain.Bookings;
using Hika.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Direction).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Comment).HasMaxLength(1000);

        builder.HasIndex(r => new { r.BookingId, r.ReviewerUserId }).IsUnique();
        builder.HasIndex(r => r.RevieweeUserId);
        // Supports AdminReviewService's unfiltered, CreatedAtUtc-sorted list — same reasoning
        // as AuditLog's CreatedAtUtc index.
        builder.HasIndex(r => r.CreatedAtUtc);

        builder.HasOne<Booking>().WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}
