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

        builder.OwnsMany(b => b.GradeScales, gbs =>
        {
            gbs.ToJson();
            gbs.Property(g => g.GradeLetter).HasMaxLength(5).IsRequired();
            gbs.Property(g => g.MinPercentage).HasPrecision(5, 2);
            gbs.Property(g => g.GpaValue).HasPrecision(4, 2);
        });

        builder.OwnsMany(b => b.LevelScales, lbs =>
        {
            lbs.ToJson();
        });

        builder.Property(b => b.MinHoursToChooseDepartment);
        builder.Property(b => b.MinHoursToChooseSpecialization);
        builder.Property(b => b.TotalHoursToCompleteDegree);
        builder.Property(b => b.MinCreditHoursPerSemester);
        builder.Property(b => b.MaxCreditHoursPerSemester);
        builder.Property(b => b.SummerMaxCreditHours);
        builder.Property(b => b.MinPassingGpa).HasPrecision(4, 2);
        builder.Property(b => b.MinPassingGradeLetter).HasMaxLength(5);
        builder.Property(b => b.MinPassingGradeSortOrder);
        builder.Property(b => b.ProbationThreshold).HasPrecision(4, 2);
        builder.Property(b => b.ProbationRegistrationLimit);
        builder.Property(b => b.MinCreditHoursForGraduationProject);
    }
}
