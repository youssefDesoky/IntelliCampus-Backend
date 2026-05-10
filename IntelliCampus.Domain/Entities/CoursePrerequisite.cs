namespace IntelliCampus.Domain.Entities;

public class CoursePrerequisite
{
    public int CourseId { get; set; }
    public int PrerequisiteCourseId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public Course PrerequisiteCourse { get; set; } = null!;
}
