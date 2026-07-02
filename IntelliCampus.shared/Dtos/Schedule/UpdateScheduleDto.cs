namespace IntelliCampus.Shared.Dtos.Schedule;

public class UpdateScheduleDto
{
    public string? Title { get; set; }
    public string? TitleAr { get; set; }
    public string? Day { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int? RoomId { get; set; }
    public int? InstructorId { get; set; }
}
