using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Student;

public class CreateStudentDto
{
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Password { get; set; }
    public string? Nationality { get; set; }
    public int? FacultyId { get; set; }
    public int? Level { get; set; }
    public string? StudentType { get; set; }
    public int? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public int? BylawId { get; set; }
    public string? BylawName { get; set; }

    public string? StudentCode { get; set; }

    public string? EnrollmentDate { get; set; }

    public StudentProgram? Program { get; set; }
    public string? ProfileImage { get; set; }
}
