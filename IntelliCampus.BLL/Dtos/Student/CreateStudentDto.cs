using System.Text.Json.Serialization;

namespace IntelliCampus.BLL.Dtos.Student;

public class CreateStudentDto
{
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string? Password { get; set; }
    public string? Nationality { get; set; }
    public string? Faculty { get; set; }
    public int? Level { get; set; }
    public int? DepartmentId { get; set; }

    [JsonPropertyName("department")]
    public string? DepartmentName { get; set; }

    [JsonPropertyName("studentId")]
    public string? StudentCode { get; set; }

    [JsonPropertyName("enrollmentDate")]
    public string? EnrollmentDate { get; set; }
}
