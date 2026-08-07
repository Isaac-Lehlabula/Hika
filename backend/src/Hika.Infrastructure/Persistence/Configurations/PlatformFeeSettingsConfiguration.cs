using Hika.Domain.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class PlatformFeeSettingsConfiguration : IEntityTypeConfiguration<PlatformFeeSettings>
{
    public void Configure(EntityTypeBuilder<PlatformFeeSettings> builder)
    {
        builder.ToTable("platform_fee_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Rate).HasColumnType("decimal(5,4)");
    }
}
