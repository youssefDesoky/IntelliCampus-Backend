namespace IntelliCampus.BLL.Dtos.Student;

public class UpdateStudentDto
{
    public string? FullName { get; set; }
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public string? Faculty { get; set; }
    public int? Level { get; set; }
    public int? DepartmentId { get; set; }
}
