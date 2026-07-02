using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Class;

public class UpdateClassDto
{
    public string? Schedule { get; set; }
    public int? RoomId { get; set; }
    public DayOfWeekEnum? Day { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int? InstructorId { get; set; }
    public int? Capacity { get; set; }
}
