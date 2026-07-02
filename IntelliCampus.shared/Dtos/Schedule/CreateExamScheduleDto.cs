using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Schedule;

public class CreateExamScheduleDto
{
    public string Day { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public ExamType ExamType { get; set; }
    public ExamStatus Status { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int? RoomId { get; set; }
}
