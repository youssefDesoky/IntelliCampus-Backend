using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class ExamSchedule
{
    public int ExamScheduleId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string? Location { get; set; }
    public ExamType ExamType { get; set; }
    public ExamStatus Status { get; set; }

    public int StudentId { get; set; }

    public int? ExamId { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;

    public Exam? Exam { get; set; }
}
