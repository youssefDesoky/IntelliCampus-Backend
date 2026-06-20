using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Course;

public class CourseDto
{
    public int CourseId { get; set; }
    public string? CourseCode { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseNameAr { get; set; }
    public int CreditHours { get; set; }
    public CourseStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int ClassCount { get; set; }
    public List<string>? Prerequisites { get; set; }

    // Fields needed by the frontend
    public string? Semester { get; set; }
    public string? Schedule { get; set; }
    public string? Room { get; set; }
    public int NumOfStudents { get; set; }
    public int TotalStudents { get; set; }
    public int WeeksCompleted { get; set; }
    public int Weeks { get; set; }
    public decimal? Attendance { get; set; }
    public decimal? Grade { get; set; }
    public decimal? CourseWork { get; set; }
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public bool IsElective { get; set; }
    public string? ProfessorName { get; set; }
}
