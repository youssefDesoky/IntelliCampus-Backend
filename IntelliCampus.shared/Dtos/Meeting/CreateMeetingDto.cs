namespace IntelliCampus.Shared.Dtos.Meeting;

public class CreateMeetingDto
{
    public string Title { get; set; } = null!;
    public DateTime DateTime { get; set; }
    public int CourseId { get; set; }
}
