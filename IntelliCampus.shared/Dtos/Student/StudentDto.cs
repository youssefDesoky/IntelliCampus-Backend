using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Student;

public class StudentDto
{
    public int StudentId { get; set; }
    public int UserId { get; set; }
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public string? StudentCode { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public int? Level { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? BylawId { get; set; }
    public string? BylawName { get; set; }
    public int? BylawVersion { get; set; }
    public string? EnrollmentDate { get; set; }
    public double Gpa { get; set; }
    public StudentProgram? Program { get; set; }
    public int? SpecializationId { get; set; }
    public string? SpecializationName { get; set; }
    public StudentType StudentType { get; set; }
    public List<string> Roles { get; set; } = [];
}
