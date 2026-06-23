namespace IntelliCampus.Domain.Entities;

public class Note
{
    public int NoteId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int? SessionId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int? MaterialFolderId { get; set; }

    // Navigation properties
    public Session? Session { get; set; }
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public MaterialFolder? MaterialFolder { get; set; }
    public NoteSummary? NoteSummary { get; set; }
}
