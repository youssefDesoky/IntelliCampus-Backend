using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ChatbotQueryConfiguration : IEntityTypeConfiguration<ChatbotQuery>
{
    public void Configure(EntityTypeBuilder<ChatbotQuery> builder)
    {
        builder.HasKey(cq => cq.QueryId);

        builder.Property(cq => cq.Question)
            .IsRequired();

        builder.HasOne(cq => cq.Student)
            .WithMany(s => s.ChatbotQueries)
            .HasForeignKey(cq => cq.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
