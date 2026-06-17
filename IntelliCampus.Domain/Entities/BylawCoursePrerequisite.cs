namespace IntelliCampus.Domain.Entities;

public class BylawCoursePrerequisite
{
    public int BylawCourseId { get; set; }
    public int PrerequisiteBylawCourseId { get; set; }
    public BylawCourse BylawCourse { get; set; } = null!;
    public BylawCourse PrerequisiteCourse { get; set; } = null!;
}
