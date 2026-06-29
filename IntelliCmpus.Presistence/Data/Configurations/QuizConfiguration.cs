using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.HasKey(q => q.QuizId);

        builder.Property(q => q.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(q => q.Description)
            .HasMaxLength(2000);

        builder.Property(q => q.StartDate)
            .IsRequired();

        builder.Property(q => q.DueDate)
            .IsRequired();

        builder.Property(q => q.MaxGrade)
            .HasPrecision(10, 2);

        builder.Property(q => q.TotalMarks)
            .IsRequired();

        builder.HasIndex(q => q.CourseId);

        builder.HasOne(q => q.Course)
            .WithMany(c => c.Quizzes)
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Questions)
            .WithOne(q => q.Quiz)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
