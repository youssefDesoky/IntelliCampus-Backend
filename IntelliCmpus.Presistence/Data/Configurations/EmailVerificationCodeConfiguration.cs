using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class EmailVerificationCodeConfiguration : IEntityTypeConfiguration<EmailVerificationCode>
{
    public void Configure(EntityTypeBuilder<EmailVerificationCode> builder)
    {
        builder.ToTable("EmailVerificationCodes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Email).IsRequired().HasMaxLength(100);
        builder.Property(c => c.CodeHash).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Purpose).IsRequired().HasMaxLength(50);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
