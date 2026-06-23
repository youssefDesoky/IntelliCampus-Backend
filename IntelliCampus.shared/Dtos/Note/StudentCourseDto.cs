namespace IntelliCampus.Shared.Dtos.Note;

public class StudentCourseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public List<StudentCourseNoteDto> Notes { get; set; } = [];
}
