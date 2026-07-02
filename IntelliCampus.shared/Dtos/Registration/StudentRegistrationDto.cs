namespace IntelliCampus.Shared.Dtos.Registration;

public class StudentRegistrationDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseCode { get; set; }
    public int CreditHours { get; set; }
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? ProfessorName { get; set; }
    public string? Day { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Room { get; set; }
    public string? Semester { get; set; }
    public DateTime RegisteredAt { get; set; }
}
