namespace IntelliCampus.shared.Dtos.Attendance;

public class SessionAttendanceDto
{
    public int SessionId { get; set; }
    public string? Topic { get; set; }
    public DateTime Date { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public List<SessionAttendanceStudentDto> Students { get; set; } = [];
}
