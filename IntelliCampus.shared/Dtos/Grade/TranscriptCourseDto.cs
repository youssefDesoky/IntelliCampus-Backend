namespace IntelliCampus.Shared.Dtos.Grade;

public class TranscriptCourseDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string? Semester { get; set; }
    public int? Level { get; set; }
    public string Coursework { get; set; } = "-";
    public string TotalGrade { get; set; } = "-";
    public string Letter { get; set; } = "-";
}
