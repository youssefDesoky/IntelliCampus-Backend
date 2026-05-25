using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Instructor;

public class UpdateInstructorDto
{
    public string? FullName { get; set; }
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public string? Role { get; set; }
    public string? Specialization { get; set; }

    public string? InstructorCode { get; set; }

    public string? DepartmentName { get; set; }

    public string? HireDate { get; set; }
}
