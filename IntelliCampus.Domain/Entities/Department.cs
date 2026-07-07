namespace IntelliCampus.Domain.Entities;

public class Department
{
    public int DepartmentId { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string DepartmentName { get; set; } = null!;
    public string? DepartmentNameAr { get; set; }
    public int? InstructorId { get; set; }
    public int? FacultyId { get; set; }
    public int? MaxCapacity { get; set; }

    // Navigation properties
    public Instructor? HeadInstructor { get; set; }
    public Faculty? Faculty { get; set; }
    public ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
    public ICollection<StudentDepartment> StudentDepartments { get; set; } = new List<StudentDepartment>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<ElectiveBucket> ElectiveBuckets { get; set; } = new List<ElectiveBucket>();
    public DepartmentRegistrationSettings? RegistrationSettings { get; set; }
}
