using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class DepartmentPreferenceConfiguration : IEntityTypeConfiguration<DepartmentPreference>
{
    public void Configure(EntityTypeBuilder<DepartmentPreference> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasOne(p => p.Student)
            .WithMany(s => s.DepartmentPreferences)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.StudentId, p.DepartmentId }).IsUnique();
    }
}
