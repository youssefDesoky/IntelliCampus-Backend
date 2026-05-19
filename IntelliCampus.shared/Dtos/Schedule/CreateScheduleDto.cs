using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Schedule;

public class CreateScheduleDto
{
    public string Title { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Location { get; set; }
    public ScheduleType Type { get; set; }
    public string? InstructorName { get; set; }
    public int? CourseId { get; set; }
    public int StudentId { get; set; }
}
