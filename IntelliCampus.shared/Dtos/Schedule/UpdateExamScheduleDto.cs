using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Schedule;

public class UpdateExamScheduleDto
{
    public string? Day { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public ExamType? ExamType { get; set; }
    public ExamStatus? Status { get; set; }
    public int? CourseId { get; set; }
    public int? RoomId { get; set; }
}
