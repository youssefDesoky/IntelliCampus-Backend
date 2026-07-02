using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Schedule
{
    public int ScheduleId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }

    // e.g. "sat", "mon"
    public string Day { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // Lecture | Section | Activity | Exam
    public ScheduleType ScheduleType { get; set; }

    public int? CourseId { get; set; }
    public int? ClassId { get; set; }
    public int? RoomId { get; set; }
    public int? InstructorId { get; set; }
    public int StudentId { get; set; }

    // Navigation properties
    public Course? Course { get; set; }
    public Class? Class { get; set; }
    public Room? Room { get; set; }
    public Instructor? Instructor { get; set; }
    public Student Student { get; set; } = null!;
}
