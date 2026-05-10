namespace IntelliCampus.Domain.Entities;

public class StudentDepartment
{
    public int DepartmentId { get; set; }
    public int StudentId { get; set; }

    // Navigation properties
    public Department Department { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
