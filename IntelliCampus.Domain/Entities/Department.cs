namespace IntelliCampus.Domain.Entities;

public class Department
{
    public int DepartmentId { get; set; }
    public string? Description { get; set; }
    public string DepartmentName { get; set; } = null!;
    public int? InstructorId { get; set; }

    // Navigation properties
    public Instructor? HeadInstructor { get; set; }
    public ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
    public ICollection<StudentDepartment> StudentDepartments { get; set; } = new List<StudentDepartment>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
