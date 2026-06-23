namespace IntelliCampus.Shared.Dtos.Note;

public class UpdateNoteDto
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public LinkedLectureDto? LinkedLecture { get; set; }
}
