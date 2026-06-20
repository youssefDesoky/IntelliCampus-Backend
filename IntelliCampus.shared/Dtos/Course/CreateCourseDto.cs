using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Course;

public class CreateCourseDto
{
    public string? CourseCode { get; set; }
    public string? CourseCodeAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }

    public string CourseName { get; set; } = null!;

    public string? CourseNameAr { get; set; }
    public int CreditHours { get; set; }

    public string? DepartmentName { get; set; }

    public List<string>? PrerequisiteCodes { get; set; }
}
