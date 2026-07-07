using IntelliCampus.Domain.Entities.Enums;
using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Student;

public class UpdateStudentDto
{
    public string? FullName { get; set; }
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public int? FacultyId { get; set; }
    public int? Level { get; set; }
    public int? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public int? BylawId { get; set; }

    public string? StudentCode { get; set; }

    public string? EnrollmentDate { get; set; }

    public StudentProgram? Program { get; set; }
    public string? ProfileImage { get; set; }
}
