namespace IntelliCampus.DAL.Entities;

public class Department
{
    public int DepartmentId { get; set; }
    public string? Description { get; set; }
    public string DepartmentName { get; set; } = null!;
    public int? InstructorId { get; set; }

    // Navigation properties
    public Instructor? HeadInstructor { get; set; }
    public ICollection<Instructor> Instructors { get; set; } = [];
    public ICollection<StudentDepartment> StudentDepartments { get; set; } = [];
    public ICollection<Course> Courses { get; set; } = [];
}
