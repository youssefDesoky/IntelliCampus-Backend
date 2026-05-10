using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Instructor;

public class CreateInstructorDto
{
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string? Password { get; set; }
    public string? Nationality { get; set; }
    public string? Role { get; set; }
    public string? Specialization { get; set; }

    [JsonPropertyName("instructorId")]
    public string? InstructorCode { get; set; }

    [JsonPropertyName("departmentId")]
    public string? DepartmentName { get; set; }

    [JsonPropertyName("HireDate")]
    public string? HireDate { get; set; }
}
