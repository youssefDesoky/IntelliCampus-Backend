using System.Text.Json.Serialization;

namespace IntelliCampus.BLL.Dtos.Course;

public class CreateCourseDto
{
    [JsonPropertyName("courseId")]
    public string? CourseCode { get; set; }

    public string CourseName { get; set; } = null!;
    public string? CourseNameAr { get; set; }
    public int CreditHours { get; set; }
    public string? Description { get; set; }

    [JsonPropertyName("departmentId")]
    public string? DepartmentName { get; set; }

    [JsonPropertyName("prerequisites")]
    public List<string>? PrerequisiteCodes { get; set; }
}
