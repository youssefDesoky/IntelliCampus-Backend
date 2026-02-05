namespace IntelliCampus.BLL.Dtos.Student;

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
    public string? Faculty { get; set; }
    public int? Level { get; set; }
}
