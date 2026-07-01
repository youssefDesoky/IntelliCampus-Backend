namespace IntelliCampus.Shared.Dtos.Reminder;

public class UpdateReminderDto
{
    public string Title { get; set; } = null!;
    public DateTime DueAt { get; set; }
    public string? Location { get; set; }

    public string Category { get; } = "personal";

    public string Priority { get; set; } = default!;
    public string SubmissionState { get; set; } = "unsubmitted";
}
