using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.HasKey(m => m.MaterialId);

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.FileUrl)
            .HasMaxLength(500);

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(m => m.CourseId);
        builder.HasIndex(m => m.FolderId);

        builder.HasOne(m => m.Course)
            .WithMany(c => c.Materials)
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Folder)
            .WithMany(f => f.Materials)
            .HasForeignKey(m => m.FolderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
