using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class InstructorMaterialConfiguration : IEntityTypeConfiguration<InstructorMaterial>
{
    public void Configure(EntityTypeBuilder<InstructorMaterial> builder)
    {
        builder.HasKey(im => new { im.InstructorId, im.MaterialId });

        builder.HasOne(im => im.Instructor)
            .WithMany(i => i.InstructorMaterials)
            .HasForeignKey(im => im.InstructorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(im => im.Material)
            .WithMany(m => m.InstructorMaterials)
            .HasForeignKey(im => im.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
