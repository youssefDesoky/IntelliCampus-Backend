using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class QrTokenConfiguration : IEntityTypeConfiguration<QrToken>
{
    public void Configure(EntityTypeBuilder<QrToken> builder)
    {
        builder.HasKey(q => q.QrTokenId);

        builder.Property(q => q.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(q => q.Token);

        builder.Property(q => q.GeneratedAt)
            .IsRequired();

        builder.Property(q => q.ExpiresAt)
            .IsRequired();

        builder.HasOne(q => q.Student)
            .WithMany()
            .HasForeignKey(q => q.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => new { q.StudentId, q.GeneratedAt });
    }
}
