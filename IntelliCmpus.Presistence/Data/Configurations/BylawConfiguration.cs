using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class BylawConfiguration : IEntityTypeConfiguration<Bylaw>
{
    public void Configure(EntityTypeBuilder<Bylaw> builder)
    {
        builder.HasKey(b => b.BylawId);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.NameAr)
            .HasMaxLength(200);

        builder.Property(b => b.Description)
            .HasMaxLength(1000);

        builder.Property(b => b.DescriptionAr)
            .HasMaxLength(1000);

        builder.Property(b => b.FileUrl)
            .HasMaxLength(500);

        builder.Property(b => b.FileName)
            .HasMaxLength(255);

        builder.HasOne(b => b.UploadedBy)
            .WithMany()
            .HasForeignKey(b => b.UploadedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.Faculty)
            .WithMany()
            .HasForeignKey(b => b.FacultyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.OwnsMany(b => b.GradeScales, gbs =>
        {
            gbs.ToJson();
            gbs.Property(g => g.GradeLetter).HasMaxLength(5).IsRequired();
            gbs.Property(g => g.MinPercentage).HasPrecision(5, 2);
            gbs.Property(g => g.GpaValue).HasPrecision(4, 2);
        });

        builder.OwnsOne(b => b.Settings, s =>
        {
            s.ToJson();
            s.OwnsMany(x => x.LevelScales);
        });

        builder.Property(b => b.MinPassingGpa).HasPrecision(4, 2);
        builder.Property(b => b.MinPassingGradeLetter).HasMaxLength(5);
        builder.Property(b => b.MinPassingGradeSortOrder);
        builder.Property(b => b.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
    }
}
