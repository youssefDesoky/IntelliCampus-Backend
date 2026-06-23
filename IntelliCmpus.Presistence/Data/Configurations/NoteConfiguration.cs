using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.HasKey(n => n.NoteId);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Content)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasOne(n => n.Session)
            .WithMany(s => s.Notes)
            .HasForeignKey(n => n.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.Student)
            .WithMany(s => s.Notes)
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Course)
            .WithMany(c => c.Notes)
            .HasForeignKey(n => n.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.MaterialFolder)
            .WithMany()
            .HasForeignKey(n => n.MaterialFolderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
