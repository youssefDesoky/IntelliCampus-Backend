namespace IntelliCampus.BLL.Dtos.Instructor;

public class InstructorDto
{
    public int InstructorId { get; set; }
    public int UserId { get; set; }
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public string? InstructorCode { get; set; }
    public string? Role { get; set; }
    public string? Specialization { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime? HireDate { get; set; }
}
