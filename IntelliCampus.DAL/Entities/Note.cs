namespace IntelliCampus.DAL.Entities;

public class Note
{
    public int NoteId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int? SessionId { get; set; }
    public int StudentId { get; set; }

    // Navigation properties
    public Session? Session { get; set; }
    public Student Student { get; set; } = null!;
    public NoteSummary? NoteSummary { get; set; }
}
