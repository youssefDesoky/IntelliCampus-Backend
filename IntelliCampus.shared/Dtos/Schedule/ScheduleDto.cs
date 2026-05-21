using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Schedule;

public class ScheduleDto
{
    public int ScheduleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? InstructorName { get; set; }
    public int? CourseId { get; set; }
    public string? CourseName { get; set; }
    public int StudentId { get; set; }
}
