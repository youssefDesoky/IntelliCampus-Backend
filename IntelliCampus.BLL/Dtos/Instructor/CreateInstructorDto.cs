namespace IntelliCampus.BLL.Dtos.Instructor;

public class CreateInstructorDto
{
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string Password { get; set; } = null!;
    public string? Nationality { get; set; }
    public string? Role { get; set; }
    public string? Specialization { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? HireDate { get; set; }
}
