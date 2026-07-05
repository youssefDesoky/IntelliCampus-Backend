namespace IntelliCampus.Shared.Dtos.Note;

public class NoteDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string CreationDate { get; set; } = null!;
    public string Modified { get; set; } = null!;
    public LinkedLectureDto? LinkedLecture { get; set; }
    public string? AiSummary { get; set; }
}
