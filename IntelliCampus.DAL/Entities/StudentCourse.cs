namespace IntelliCampus.DAL.Entities;

public class StudentCourse
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int? ClassId { get; set; }
    public string? Semester { get; set; }
    public DateTime RegisteredAt { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Class? Class { get; set; }
}
