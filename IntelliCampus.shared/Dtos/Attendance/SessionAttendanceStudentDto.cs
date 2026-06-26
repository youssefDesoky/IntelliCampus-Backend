using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class SessionAttendanceStudentDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
    public DateTime? CheckInTime { get; set; }
}
