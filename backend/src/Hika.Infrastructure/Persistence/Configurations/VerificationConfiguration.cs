using Hika.Domain.TrustSafety;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class VerificationConfiguration : IEntityTypeConfiguration<Verification>
{
    public void Configure(EntityTypeBuilder<Verification> builder)
    {
        builder.ToTable("verifications");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.SubjectType).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.DocumentUrl).HasMaxLength(2048);
        builder.Property(v => v.RejectionReason).HasMaxLength(1000);

        builder.HasIndex(v => new { v.SubjectType, v.SubjectId, v.Type });
        builder.HasIndex(v => v.Status);
    }
}
