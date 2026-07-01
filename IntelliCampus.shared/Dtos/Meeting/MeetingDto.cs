namespace IntelliCampus.Shared.Dtos.Meeting;

public class MeetingDto
{
    public int MeetingId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime DateTime { get; set; }
    public string RoomName { get; set; } = null!;
    public int CourseId { get; set; }
    public int? InstructorId { get; set; }
    public bool IsActive { get; set; }
}
