using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Class
{
    public int ClassId { get; set; }
    public string? GroupCode { get; set; }
    public string? GroupCodeAr { get; set; }
    public ClassType ClassType { get; set; }
    public DayOfWeekEnum? Day { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int? RoomId { get; set; }
    public int CourseId { get; set; }
    public int? InstructorId { get; set; }
    public int? Capacity { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public Instructor? Instructor { get; set; }
    public Room? Room { get; set; }
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
}