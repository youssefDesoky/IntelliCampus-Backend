using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Class
{
    public int ClassId { get; set; }
    public string? GroupCode { get; set; }
    public ClassType ClassType { get; set; }
    public DayOfWeekEnum? Day { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Room { get; set; }
    public int CourseId { get; set; }
    public int? InstructorId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public Instructor? Instructor { get; set; }
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
