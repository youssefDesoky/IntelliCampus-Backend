using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Reminder
{
    public int ReminderId { get; set; }

    public int? StudentId { get; set; }

    public int? InstructorId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime Date { get; set; }

    public ReminderType Type { get; set; }

    public string? Location { get; set; }

    public string Priority { get; set; } = "low";

    public SubmissionState State { get; set; } = SubmissionState.Unsubmitted;

    // Navigation properties
    public Student? Student { get; set; }

    public Instructor? Instructor { get; set; }
}
