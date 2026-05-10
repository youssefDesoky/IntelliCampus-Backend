using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.HasKey(n => n.NoteId);

        builder.Property(n => n.Content)
            .IsRequired();

        builder.HasOne(n => n.Session)
            .WithMany(s => s.Notes)
            .HasForeignKey(n => n.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.Student)
            .WithMany(s => s.Notes)
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
