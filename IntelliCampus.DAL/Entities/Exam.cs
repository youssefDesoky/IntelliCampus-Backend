namespace IntelliCampus.DAL.Entities;

public class Exam
{
    public int ExamId { get; set; }
    public string? Location { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public int CourseId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
}
