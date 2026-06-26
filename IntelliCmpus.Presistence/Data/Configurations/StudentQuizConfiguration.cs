using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class StudentQuizConfiguration : IEntityTypeConfiguration<StudentQuiz>
{
    public void Configure(EntityTypeBuilder<StudentQuiz> builder)
    {
        builder.HasKey(sq => new { sq.StudentId, sq.QuizId });

        builder.Property(sq => sq.Score)
            .HasPrecision(5, 2);

        builder.Property(sq => sq.StartedAt);

        builder.Property(sq => sq.AnswersJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(sq => sq.QuestionResultsJson)
            .HasColumnType("nvarchar(max)");

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
