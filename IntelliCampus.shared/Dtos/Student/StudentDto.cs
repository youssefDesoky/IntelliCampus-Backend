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
    public string? Faculty { get; set; }
    public int? Level { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? BaylawId { get; set; }
    public string? BaylawName { get; set; }
    public int? BaylawVersion { get; set; }
    public DateTime? EnrollmentDate { get; set; }
}
