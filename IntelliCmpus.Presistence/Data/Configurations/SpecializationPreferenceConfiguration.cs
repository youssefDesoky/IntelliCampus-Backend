using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class SpecializationPreferenceConfiguration : IEntityTypeConfiguration<SpecializationPreference>
{
    public void Configure(EntityTypeBuilder<SpecializationPreference> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TargetType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasOne(p => p.Student)
            .WithMany(s => s.SpecializationPreferences)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.StudentId, p.TargetId }).IsUnique();
    }
}
