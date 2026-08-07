using Hika.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hika.Infrastructure.Persistence.Configurations;

public sealed class PhoneVerificationCodeConfiguration : IEntityTypeConfiguration<PhoneVerificationCode>
{
    public void Configure(EntityTypeBuilder<PhoneVerificationCode> builder)
    {
        builder.ToTable("phone_verification_codes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(c => c.CodeHash).HasMaxLength(128).IsRequired();

        builder.HasIndex(c => new { c.UserId, c.UsedAtUtc });
    }
}
