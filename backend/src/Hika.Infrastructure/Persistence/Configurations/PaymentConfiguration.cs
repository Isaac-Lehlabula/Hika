using Hika.Domain.Bookings;
using Hika.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Provider).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.ProviderReference).HasMaxLength(100);

        builder.ComplexProperty(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("amount_currency").HasConversion<string>().HasMaxLength(3).IsRequired();
        });
        builder.ComplexProperty(p => p.PlatformFee, money =>
        {
            money.Property(m => m.Amount).HasColumnName("platform_fee_amount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("platform_fee_currency").HasConversion<string>().HasMaxLength(3).IsRequired();
        });
        builder.ComplexProperty(p => p.DriverPayoutAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("driver_payout_amount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("driver_payout_currency").HasConversion<string>().HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(p => p.BookingId).IsUnique();

        builder.HasOne<Booking>().WithMany().HasForeignKey(p => p.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}
