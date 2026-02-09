using System.Text.Json.Serialization;

namespace IntelliCampus.BLL.Dtos.Course;

public class CreateCourseDto
{
    [JsonPropertyName("id")]
    public string? CourseCode { get; set; }

    [JsonPropertyName("title")]
    public string CourseName { get; set; } = null!;

    public string? CourseNameAr { get; set; }
    public int CreditHours { get; set; }

    [JsonPropertyName("department")]
    public string? DepartmentName { get; set; }

    [JsonPropertyName("prerequisites")]
    public List<string>? PrerequisiteCodes { get; set; }
}
