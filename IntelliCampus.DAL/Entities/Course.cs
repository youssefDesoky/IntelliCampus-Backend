using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.DAL.Entities;

public class Course
{
    public int CourseId { get; set; }
    public int CreditHours { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseNameAr { get; set; }
    public CourseStatus Status { get; set; }

    // Navigation properties
    public ICollection<Class> Classes { get; set; } = [];
    public ICollection<Grade> Grades { get; set; } = [];
    public ICollection<Exam> Exams { get; set; } = [];
    public ICollection<StudentCourse> StudentCourses { get; set; } = [];
    public ICollection<CoursePrerequisite> Prerequisites { get; set; } = [];
    public ICollection<CoursePrerequisite> PrerequisiteFor { get; set; } = [];
}
