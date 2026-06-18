using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.ToTable("Faculties");
        builder.HasKey(f => f.FacultyId);

        builder.Property(f => f.FacultyName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.FacultyNameAr)
            .HasMaxLength(100)
            .IsUnicode();

        builder.Property(f => f.FacultyCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.HasIndex(f => f.FacultyCode).IsUnique();

        builder.Property(f => f.Description)
            .HasMaxLength(500);
    }
}
