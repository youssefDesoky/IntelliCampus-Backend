using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Schedule
{
    public int ScheduleId { get; set; }
    public ScheduleType Type { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DayOfWeekEnum Day { get; set; }
    public string? Location { get; set; }
    public int StudentId { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
}
