using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Exam;

public class CreateExamDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public ExamType ExamType { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public int DurationMinutes { get; set; }
    public decimal MaxGrade { get; set; }
    public int? TotalMarks { get; set; }
    public int? RoomId { get; set; }
    public int CourseId { get; set; }
}
