using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Course
{
    public int CourseId { get; set; }
    public string? CourseCode { get; set; }
    public int CreditHours { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseNameAr { get; set; }
    public CourseStatus Status { get; set; }
    public int? DepartmentId { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    public ICollection<CoursePrerequisite> Prerequisites { get; set; } = new List<CoursePrerequisite>();
    public ICollection<CoursePrerequisite> PrerequisiteFor { get; set; } = new List<CoursePrerequisite>();
    public ICollection<Material> Materials { get; set; } = new List<Material>();
    public ICollection<MaterialFolder> MaterialFolders { get; set; } = new List<MaterialFolder>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}