using IntelliCampus.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.DAL.Data.Configurations;

public class StudentQuizConfiguration : IEntityTypeConfiguration<StudentQuiz>
{
    public void Configure(EntityTypeBuilder<StudentQuiz> builder)
    {
        builder.HasKey(sq => new { sq.StudentId, sq.QuizId });

        builder.Property(sq => sq.Score)
            .HasPrecision(5, 2);

        builder.HasOne(sq => sq.Student)
            .WithMany(s => s.StudentQuizzes)
            .HasForeignKey(sq => sq.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sq => sq.Quiz)
            .WithMany(q => q.StudentQuizzes)
            .HasForeignKey(sq => sq.QuizId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
