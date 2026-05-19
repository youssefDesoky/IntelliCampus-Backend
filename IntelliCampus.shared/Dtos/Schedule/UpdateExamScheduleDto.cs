using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Schedule;

public class UpdateExamScheduleDto
{
    public string? CourseCode { get; set; }
    public string? CourseName { get; set; }
    public string? Day { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Location { get; set; }
    public ExamType? ExamType { get; set; }
    public ExamStatus? Status { get; set; }
}
