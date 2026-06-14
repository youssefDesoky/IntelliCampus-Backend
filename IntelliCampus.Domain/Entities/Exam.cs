using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Exam
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
    public int CourseId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public Room? Room { get; set; }
    public ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();
    public ICollection<ExamSeatAssignment> ExamSeatAssignments { get; set; } = new List<ExamSeatAssignment>();
}
