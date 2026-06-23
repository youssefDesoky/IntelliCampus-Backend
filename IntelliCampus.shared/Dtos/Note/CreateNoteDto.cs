namespace IntelliCampus.Shared.Dtos.Note;

public class CreateNoteDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public LinkedLectureDto? LinkedLecture { get; set; }
}
