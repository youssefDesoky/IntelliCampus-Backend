using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Schedule;

public class ExamScheduleDto
{
    public int ExamScheduleId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseCodeAr { get; set; }
    public string? CourseName { get; set; }
    public string? CourseNameAr { get; set; }
    public string Day { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? LocationAr { get; set; }
    public ExamType ExamType { get; set; }
    public ExamStatus Status { get; set; }
    public int StudentId { get; set; }
    public int? RoomId { get; set; }
}
