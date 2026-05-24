using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class ManualAttendanceDto
{
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
}
