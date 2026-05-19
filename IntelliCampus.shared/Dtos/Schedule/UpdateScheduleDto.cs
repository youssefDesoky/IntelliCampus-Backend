namespace IntelliCampus.Shared.Dtos.Schedule;

public class UpdateScheduleDto
{
    public string? Title { get; set; }
    public string? Day { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Location { get; set; }
    public string? InstructorName { get; set; }
}
