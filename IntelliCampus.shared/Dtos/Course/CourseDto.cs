using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Course;

public class CourseDto
{
    public int CourseId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseCodeAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
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
    public string? SemesterAr { get; set; }
    public string? Schedule { get; set; }
    public string? ScheduleAr { get; set; }
    public string? Room { get; set; }
    public string? RoomAr { get; set; }
    public int NumOfStudents { get; set; }
    public int TotalStudents { get; set; }
    public int WeeksCompleted { get; set; }
    public int Weeks { get; set; }
    public decimal? Attendance { get; set; }
    public string? Grade { get; set; }
    public decimal? TotalGrade { get; set; }
    public decimal? CourseWork { get; set; }
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? ClassNameAr { get; set; }
    public bool IsElective { get; set; }
    public string? ProfessorName { get; set; }
    public string? ProfessorNameAr { get; set; }

    public string? StudentCourseStatusName { get; set; }

    // Registration settings
    public DateTime? RegistrationStartDate { get; set; }
    public DateTime? RegistrationEndDate { get; set; }
    public List<int>? AllowedLevels { get; set; }
    public List<int>? AllowedDepartments { get; set; }
}
