using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Exam;

public class ExamDto
{
    public int ExamId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public ExamType ExamType { get; set; }
    public ExamStatus Status { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public int DurationMinutes { get; set; }
    public decimal MaxGrade { get; set; }
    public int? TotalMarks { get; set; }
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public string? RoomNameAr { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? CourseNameAr { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseCodeAr { get; set; }
    public DateTime CreatedAt { get; set; }
}
