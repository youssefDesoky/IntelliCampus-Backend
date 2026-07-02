using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Schedule;

public class CreateScheduleDto
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string Day { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public ScheduleType Type { get; set; }
    public int? CourseId { get; set; }
    public int? RoomId { get; set; }
    public int? InstructorId { get; set; }
    public int StudentId { get; set; }
}
