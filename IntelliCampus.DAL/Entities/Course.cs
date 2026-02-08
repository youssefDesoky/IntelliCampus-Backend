using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.DAL.Entities;

public class Course
{
    public int CourseId { get; set; }
    public string? CourseCode { get; set; }
    public int CreditHours { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseNameAr { get; set; }
    public string? Description { get; set; }
    public CourseStatus Status { get; set; }
    public int? DepartmentId { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
    public ICollection<Class> Classes { get; set; } = [];
    public ICollection<Grade> Grades { get; set; } = [];
    public ICollection<Exam> Exams { get; set; } = [];
    public ICollection<StudentCourse> StudentCourses { get; set; } = [];
    public ICollection<CoursePrerequisite> Prerequisites { get; set; } = [];
    public ICollection<CoursePrerequisite> PrerequisiteFor { get; set; } = [];
    public ICollection<Material> Materials { get; set; } = [];
    public ICollection<MaterialFolder> MaterialFolders { get; set; } = [];
}
