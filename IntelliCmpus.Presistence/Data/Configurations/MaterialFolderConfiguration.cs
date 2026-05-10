using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class MaterialFolderConfiguration : IEntityTypeConfiguration<MaterialFolder>
{
    public void Configure(EntityTypeBuilder<MaterialFolder> builder)
    {
        builder.HasKey(f => f.MaterialFolderId);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Description)
            .HasMaxLength(500);

        builder.HasOne(f => f.Course)
            .WithMany(c => c.MaterialFolders)
            .HasForeignKey(f => f.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.CreatedByInstructor)
            .WithMany(i => i.CreatedFolders)
            .HasForeignKey(f => f.CreatedByInstructorId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
