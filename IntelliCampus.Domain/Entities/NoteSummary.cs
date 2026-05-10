namespace IntelliCampus.Domain.Entities;

public class NoteSummary
{
    public int SummaryId { get; set; }
    public string GeneratedText { get; set; } = null!;
    public int NoteId { get; set; }

    // Navigation properties
    public Note Note { get; set; } = null!;
}
