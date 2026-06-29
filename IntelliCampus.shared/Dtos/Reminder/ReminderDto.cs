namespace IntelliCampus.Shared.Dtos.Reminder;

public class ReminderDto
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime DueAt { get; set; }
    public string Location { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Priority { get; set; } = default!;
    public string SubmissionState { get; set; } = "unsubmitted";
}