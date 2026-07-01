namespace IntelliCampus.Domain.Entities;

public class Meeting
{
    public int MeetingId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime DateTime { get; set; }
    public string RoomName { get; set; } = null!;
    public int CourseId { get; set; }
    public int? InstructorId { get; set; }
    public bool IsActive { get; set; } = true;

    public Course Course { get; set; } = null!;
    public Instructor? Instructor { get; set; }
}
