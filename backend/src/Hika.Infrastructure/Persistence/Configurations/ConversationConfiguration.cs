using Hika.Domain.Bookings;
using Hika.Domain.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(c => c.Id);

        // One conversation per booking, ever — enforced at the DB level, not just by
        // ChatService's Local-then-query existence check.
        builder.HasIndex(c => c.BookingId).IsUnique();

        builder.HasOne<Booking>().WithMany().HasForeignKey(c => c.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}
